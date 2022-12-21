using System.Diagnostics;
using System.Text;
using System.Text.Json;
using WDCMonInterface.CompilerIntegration;
using WDCMonInterface.DAP;
using WDCMonInterface.Devices;

#pragma warning disable IDE1006 // Naming Styles

namespace WDCMonInterface
{
    public class PluginConfiguration
    {
        public string? type { get; set; }
        public string? request { get; set; }
        public string? name { get; set; }
        public string? program { get; set; }
        public string? port { get; set; }
        public string? startSymbol { get; set; }
        public string? linkerConfig { get; set; }
    }

    internal class Program
    {
        static TResult? JsonCast<TResult, TSource>(TSource a)
        {
            string jsonString = System.Text.Json.JsonSerializer.Serialize<TSource>(a, new System.Text.Json.JsonSerializerOptions() { DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull });
            return System.Text.Json.JsonSerializer.Deserialize<TResult>(jsonString);
        }

        static DapHandler dap = new(new MemoryStream(), new MemoryStream());
        static void Main(string[] args)
        {
            string logDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "vscode_6502_debugger");
            Directory.CreateDirectory(logDirectory);
            string logFile = Path.Combine(logDirectory, "log.txt");
            if (File.Exists(logFile)) try
                {
                    File.Delete(logFile);
                }
                catch (Exception) { }
            Logger.FileName = logFile;


            Logger.Log("WdcMonInterface started!");
            dap = new DapHandler(Console.OpenStandardInput(), Console.OpenStandardOutput());
            dap.OnDapMessageReceived += DapMessageReceived;
            dap.Run();
        }

        private static bool Initialized = false;
        static AdapterInformation? adapterInfos = null;

        private static bool Launched = false;
        static Debugger? debugger = null;
        static DbgFile? debugInformation = null;
        static PluginConfiguration? myConfig = null;
        static ushort nextInstruction = 0;

        static int sequence = 1;

        static readonly List<DapBreakpoint> ConfiguredBreakpoints = new();

        static void Respond<TBody>(RequestMessage request, TBody body)
        {
            ResponseMessage responseMessage = new()
            {
                request_seq = request.seq,
                command = request.command,
                seq = sequence++,
                success = true,
                type = "response",
                message = null,
            };
            if (body != null)
            {
                responseMessage.body = JsonCast<Dictionary<string, JsonElement>, TBody>(body);
            }
            dap.SendMessage(new DapMessage() { Message = responseMessage });
        }

        static void RespondError(RequestMessage request, string message)
        {
            ResponseMessage responseMessage = new()
            {
                request_seq = request.seq,
                command = request.command,
                seq = sequence++,
                success = false,
                type = "response",
                message = message,
            };
            responseMessage.body = JsonCast<Dictionary<string, JsonElement>, object>(new
            {
                message = new
                {
                    id = 0,
                    format = "string",
                    sendTelemetry = false,
                    showUser = true,
                }
            });
            dap.SendMessage(new DapMessage() { Message = responseMessage });
        }

        static void SendEvent<TBody>(TBody body, string eventType)
        {
            EventMessage eventMessage = new()
            {
                type = "event",
                //seq = sequence++,
                Event = eventType,
            };
            if (body != null)
            {
                eventMessage.body = JsonCast<Dictionary<string, JsonElement>, TBody>(body);
            }
            dap.SendMessage(new DapMessage() { Message = eventMessage });
        }

        static readonly int registerScopeId = 6502;

        private static void DapMessageReceived(DapHandler handler, DapMessage dm)
        {
            if (dm.Message is RequestMessage request)
            {
                if (request?.command?.ToLower() != "initialize" && !Initialized)
                {
                    Logger.Log($"Error: Request {request?.command} sent without calling initialize first!");
                    return;
                }

                switch (request?.command?.ToLower())
                {
                    //  The initialize request is sent as the first request from the client to the debug adapter in order to configure it with client capabilities and to retrieve capabilities from the debug adapter.
                    case "initialize":
                        if (Initialized)
                        {
                            Logger.Log("Error: Initialize received after initialization has already occurred.");
                            break;
                        }
                        adapterInfos = JsonCast<AdapterInformation, Dictionary<string, JsonElement>>(request.arguments ?? new Dictionary<string, JsonElement>());
                        Logger.Log($"Initialize called from IDE {adapterInfos?.clientName}");
                        Initialized = true;
                        Respond(request, new
                        {
                            supportsSetVariable = true,
                            supportsConfigurationDoneRequest = true,
                            supportsBreakpointLocationsRequest = true,
                            exceptionBreakpointFilters = Array.Empty<object>(),
                            supportsEvaluateForHovers = true,
                        });

                        SendEvent<string?>(null, "initialized");
                        break;
                    // The disconnect request asks the debug adapter to disconnect from the debuggee (thus ending the debug session) and then to shut down itself (the debug adapter).
                    case "disconnect":
                        Logger.Log("Stopping...");
                        dap.ShouldRun = false;
                        break;
                    // Sets multiple breakpoints for a single source and clears all previous breakpoints in that source.
                    case "setbreakpoints":
                        {
                            SetBreakpointsRequest? setBrkReq = JsonCast<SetBreakpointsRequest, Dictionary<string, JsonElement>>(request.arguments ?? new Dictionary<string, JsonElement>());
                            DapBreakpoint[] toRemove = Array.Empty<DapBreakpoint>();
                            DapBreakpoint[] toAdd = Array.Empty<DapBreakpoint>();
                            if (setBrkReq != null)
                            {
                                toRemove = ConfiguredBreakpoints.Where(a => (a.source != null) && a.source == setBrkReq.source).ToArray();
                                foreach (var remove in toRemove) ConfiguredBreakpoints.Remove(remove);
                                toAdd = setBrkReq.breakpoints?.Select(s => new DapBreakpoint() { source = setBrkReq.source, line = s.line, verified = false, message = "Break Execution to set Breakpoints!" }).ToArray() ?? Array.Empty<DapBreakpoint>();
                                ConfiguredBreakpoints.AddRange(toAdd);
                            }

                            if (Launched)
                            {
                                if (debugger?.State == Debugger.DebuggerState.RUNNING)
                                {
                                    debugger.DoBeforeNextContinue(() =>
                                    {
                                        DebuggerRemoveBreakpoints(toRemove, true);
                                        DebuggerAddBreakpoints(toAdd, true);
                                    });
                                }
                                else
                                {
                                    DebuggerRemoveBreakpoints(toRemove, false);
                                    DebuggerAddBreakpoints(toAdd, false);
                                }
                            }

                            Respond(request, new { breakpoints = toAdd });
                        }
                        break;
                    case "breakpointlocations":
                        {
                            var bpLocReq = JsonCast<BreakpointLocationsRequest, Dictionary<string, JsonElement>>(request.arguments ?? new Dictionary<string, JsonElement>());

                            if (bpLocReq == null)
                            {
                                Respond(request, new { breakpoints = Array.Empty<object>() });
                            }
                            else
                            {
                                Respond(request, new
                                {
                                    breakpoints = ConfiguredBreakpoints.Where(s => (s?.source != null) && s.source == bpLocReq?.source && bpLocReq.line <= s.line && (!bpLocReq.endLine.HasValue || bpLocReq.endLine.Value >= s.line)).Select(s => new { s.line, }).ToArray(),
                                });
                            }
                        }
                        break;
                    case "setexceptionbreakpoints":
                        Respond(request, new { breakpoints = Array.Empty<DapBreakpoint>() });
                        break;
                    case "configurationdone":
                        Respond<object?>(request, null);
                        break;
                    // This launch request is sent from the client to the debug adapter to start the debuggee with or without debugging (if noDebug is true).
                    case "launch":
                    case "attach":
                        {
                            // parse commands, create wdcmon with port
                            // compile source files, create debugger-item
                            // DebuggerAddBreakpoints(all breakpoints) (updates them in the UI)
                            // Begin at the starting point.
                            myConfig = JsonCast<PluginConfiguration, Dictionary<string, JsonElement>>(request.arguments ?? new Dictionary<string, JsonElement>());
                            if (String.IsNullOrWhiteSpace(myConfig?.program) || String.IsNullOrWhiteSpace(myConfig?.port) || String.IsNullOrWhiteSpace(myConfig?.startSymbol))
                            {
                                if (String.IsNullOrWhiteSpace(myConfig?.program)) DebugConsole("Please enter the program's source file in the launch configuration.");
                                else if (!File.Exists(myConfig?.program)) DebugConsole($"The file {myConfig?.program} does not exist.");
                                if (String.IsNullOrWhiteSpace(myConfig?.port)) DebugConsole("Please enter a serial port to connect to a device.");
                                if (String.IsNullOrWhiteSpace(myConfig?.startSymbol)) DebugConsole("Please enter the name of your program's 'Start'-Symbol. This should be the name of the label where your program starts execution (e.g. 'RESET')");

                                RespondError(request, "Bad configuration. Please see the debug console for errors.");
                                break;
                            }
                            try
                            {
                                WDCMon wdcmon = new(myConfig.port);
                                DebugConsole($"Connected to device at {myConfig.port}");
                                Logger.Log($"Device connected at Port {myConfig.port}");

                                Stopwatch sw = new Stopwatch();
                                sw.Start();

                                string debugFileName;
                                try
                                {
                                    string? dbg = Compiler.CompileAssembly(myConfig.program, myConfig.linkerConfig);
                                    if (string.IsNullOrWhiteSpace(dbg) || !File.Exists(dbg))
                                    {
                                        throw new Exception("Compile-error: no output or invalid debug file name.");
                                    }
                                    debugFileName = dbg;
                                }
                                catch (Exception ex)
                                {
                                    Logger.Log("compile error");
                                    Logger.Log(ex);
                                    if (ex.GetType() != typeof(Exception))
                                    {
                                        DebugConsole($"There was an error: {ex.Message}");
                                    }
                                    RespondError(request, "Error compiling the program. please see the debug console for further information.");
                                    break;

                                }

                                sw.Stop();

                                DebugConsole($"{myConfig.program} -> {debugFileName}");
                                DebugConsole($"compiled program in {(sw.ElapsedMilliseconds / 1000.0)}s");

                                Logger.Log($"Compiled; output: {debugFileName}");
                                debugInformation = new DbgFile(debugFileName);
                                Logger.Log($"Loaded debug Information: {debugInformation.Symbols.Count} Symbols");
                                debugger = new Debugger(wdcmon, debugInformation);
                                Logger.Log($"Program written to device.");
                                DebugConsole($"Program loaded to device (${debugger.numLoadedBytes:X4} bytes in {debugger.numLoadedSegments} segments)");
                                Launched = true;

                                string symStart = myConfig.startSymbol?.Trim() ?? "";
                                var symbol = debugInformation.Symbols.Select(s => s.Value).FirstOrDefault(s => s?.Name?.ToLower() == symStart.ToLower());
                                if (symbol != null)
                                {
                                    nextInstruction = (ushort)symbol.value;
                                    Logger.Log($"Starting at: ${nextInstruction:X4}");
                                    DebugConsole($"Resolved start symbol '{symbol.Name}' as location ${nextInstruction:X4}");
                                }
                                else
                                {
                                    nextInstruction = 0;
                                    RespondError(request, $"Could not resolve symbol '{symStart}'");
                                    break;
                                }

                            }
                            catch (Exception ex)
                            {
                                Logger.Log("Error connecting to device");
                                Logger.Log(ex);
                                RespondError(request, $"Could not connect to device at {myConfig.port}: \n{ex.Message}");
                                break;
                            }

                            Respond<object?>(request, null);

                            Stopped("entry");
                        }
                        break;
                    // The request resumes execution of all threads.
                    case "continue":
                        if (Initialized && debugger != null)
                        {
                            if (debugger.State != Debugger.DebuggerState.RUNNING)
                            {
                                if (debugger.State == Debugger.DebuggerState.IDLE)
                                {
                                    Respond(request, new { allThreadsContinued = true, });
                                    OnHalted(debugger.Execute(nextInstruction));
                                }
                                else
                                {
                                    Respond(request, new { allThreadsContinued = true, });
                                    OnHalted(debugger.ContinueExecution(nextInstruction));
                                }
                            }
                            else
                            {
                                Respond(request, new { allThreadsContinued = true, });
                            }
                        }
                        else RespondError(request, "unknown error");
                        break;
                    // The request executes one step (in the given granularity) for the specified thread and allows all other threads to run freely by resuming them.
                    case "next": // "step Over"
                        if (debugger != null)
                        {
                            Respond(request, new { });
                            OnHalted(debugger.StepOver(nextInstruction));
                        }
                        else RespondError(request, "not initialized");
                        break;
                    //The request resumes the given thread to step into a function/method and allows all other threads to run freely by resuming them.
                    case "stepin": // "step Into"
                    case "stepout": // -> "step out", we fake this.
                        if (debugger != null)
                        {
                            OnHalted(debugger.StepInto(nextInstruction));
                        }
                        else RespondError(request, "not initialized");
                        break;
                    // The request suspends the debuggee.
                    case "pause":
                        RespondError(request, "To pause the program, press the 'stop'-button on the device.");
                        break;
                    //  The request returns a stacktrace from the current execution state of a given thread.
                    case "stacktrace":
                        if (debugger != null && debugInformation != null)
                        {
                            var line = debugInformation.LineFromAddress(nextInstruction);
                            if (line != null)
                            {
                                var file = debugInformation.FileName(line);
                                Respond(request, new
                                {
                                    stackFrames = new object[] {
                                            new
                                            {
                                                id = 0,//nextStackFrameId++,
                                                name = $"${nextInstruction:X4}",
                                                source = new DapSource()
                                                {
                                                     name = Path.GetFileName(file),
                                                     path = file,
                                                     sourceReference = 0,
                                                },
                                                line = (adapterInfos?.columnsStartAt1 != false) ? line.line_number : line.line_number - 1,
                                                column = (adapterInfos?.columnsStartAt1 != false) ? 1 : 0,
                                            }
                                        },
                                    totalFrames = 1
                                });
                                break;
                            }
                        }

                        Respond(request, new
                        {
                            stackFrames = Array.Empty<object>(),
                            totalFrames = 0
                        });
                        break;
                    // The request returns the variable scopes for a given stack frame ID.
                    case "scopes":
                        var myScope = debugInformation?.ScopeFromAddress(nextInstruction);
                        if (myScope == null)
                        {
                            Respond(request, new
                            {
                                scopes = Array.Empty<object>()
                            });
                            break;
                        }

                        Respond(request, new
                        {
                            scopes = new object[]
                            {
                                new
                                {
                                    name = "Registers",
                                    variablesReference = registerScopeId,
                                    expensive = false,
                                    presentationHint = "registers",
                                }
                            }
                        });
                        break;
                    // Retrieves all child variables for the given variable reference.  
                    case "variables":
                        if (debugger != null)
                        {
                            // check for scope id.
                            int variablesReference = 0;
                            if (request.arguments?.TryGetValue("variablesReference", out JsonElement val) == true)
                            {
                                variablesReference = (int)val.GetDouble();
                            }

                            Respond(request, new
                            {
                                variables = VariablesInScope(variablesReference)
                            });
                        }
                        else
                        {
                            RespondError(request, "To pause the program, press the 'stop'-button on the device.");
                        }
                        break;
                    // Set the variable with the given name in the variable container to a new value. Clients should only call this request if the corresponding capability supportsSetVariable is true.
                    case "setvariable":
                        {
                            var setVarReq = JsonCast<SetVariableRequest, Dictionary<string, JsonElement>>(request.arguments ?? new Dictionary<string, JsonElement>());
                            if (setVarReq == null) RespondError(request, "Invalid request");
                            else
                            {
                                var newValue = SetVariableRequest(setVarReq);
                                if (String.IsNullOrWhiteSpace(newValue))
                                {
                                    newValue = GetVariable(setVarReq.variablesReference ?? 0, setVarReq.name ?? "")?.value;
                                }

                                Respond(request, new
                                {
                                    value = newValue ?? "",
                                });
                            }
                        }
                        break;
                    // The request retrieves a list of all threads.
                    case "threads":
                        Respond(request, new
                        {
                            threads = new List<object>
                            {
                                new {
                                    id = 1,
                                    name = "Thread: 6502"
                                },
                            }
                        });
                        break;
                    case "source":
                        {
                            var gsa = JsonCast<GetSourceArguments, Dictionary<string, JsonElement>>(request.arguments ?? new Dictionary<string, JsonElement>());
                            Respond(request, new GetSourceResponse(gsa?.source?.path ?? ""));
                        }
                        break;
                    // The request sets the location where the debuggee will continue to run.
                    case "goto":
                        {
                            if (request != null)
                            {
                                if (request.arguments != null && request.arguments.ContainsKey("targetId") && debugInformation != null)
                                {
                                    var targetId = (int)request.arguments["targetId"].GetDouble();
                                    if (debugInformation.Lines.ContainsKey(targetId))
                                    {
                                        var targetLine = debugInformation.Lines[targetId];
                                        if (targetLine != null && targetLine.span != null && debugInformation.Spans.ContainsKey(targetLine.span.Value))
                                        {
                                            var span = debugInformation.Spans[targetLine.span.Value];
                                            var segment = debugInformation.Segments[span.segment_id];
                                            ushort targetAddress = (ushort)(segment.Start + span.start);
                                            nextInstruction = targetAddress;
                                        }
                                    }
                                }
                                Respond<object>(request, null);
                            }
                        }
                        break;
                    case "qototargets":
                        {
                            var targetsReq = JsonCast<GotoTargetsRequest, Dictionary<string, JsonElement>>(request.arguments ?? new Dictionary<string, JsonElement>());
                            var file = debugInformation?.Files.Select(s => s.Value).FirstOrDefault(f => f.Name?.ToLower() == targetsReq?.Source?.path?.ToLower()?.Replace("\\", "/"));
                            if (targetsReq == null || targetsReq.Source == null || debugInformation == null || file == null)
                            {
                                Respond(request, new
                                {
                                    targets = Array.Empty<object>()
                                });
                                break;
                            }

                            var lines = debugInformation.Lines.Select(s => s.Value).Where(s => s.file_id == file.ID && s.span != null).ToArray();
                            Respond(request, new
                            {
                                targets = lines.Select(s => new
                                {
                                    id = s.ID,
                                    label = format16((ushort)debugInformation.Spans[s?.span ?? 0].start),
                                    line = (adapterInfos?.linesStartAt1 != false ? s?.line_number : s?.line_number - 1)
                                }).ToArray()
                            });
                        }
                        break;
                    //  The request retrieves the source code for a given source reference.
                    // Evaluates the given expression in the context of the topmost stack frame.
                    case "evaluate":
                        {
                            EvaluateRequest? evalReq = JsonCast<EvaluateRequest, Dictionary<string, JsonElement>>(request.arguments ?? new Dictionary<string, JsonElement>());
                            if (debugger == null || evalReq == null || String.IsNullOrWhiteSpace(evalReq.expression) || String.IsNullOrWhiteSpace(evalReq.context))
                            {
                                RespondError(request, "bad arguments");
                            }
                            else
                            {
                                if (evalReq.context == "hover")
                                {
                                    // evaluate Labels
                                    var scope = debugInformation?.ScopeFromAddress(nextInstruction);
                                    var symbol = debugInformation?.Symbols.Where(s => (scope == null || s.Value.scope_id == scope.ID || s.Value.scope_id < 0) && s.Value?.Name?.ToLower() == evalReq.expression.ToLower()).Select(s => s.Value).FirstOrDefault();
                                    if (symbol != null)
                                    {
                                        string result = format16((ushort)symbol.value);
                                        if (symbol.type == "lab")
                                        {
                                            result = $"{result}: {format8(debugger?.ReadMemory((ushort)symbol.value, 1)[0] ?? 0)}";
                                        }
                                        // found symbol
                                        Respond(request, new
                                        {
                                            result,
                                            variablesReference = 0,
                                        });
                                        break;
                                    }
                                }
                                else
                                {
                                    if (evalReq.expression.Trim().StartsWith("?"))
                                    {
                                        // Monitor
                                        string monitorExpression = evalReq.expression.Trim().Trim('?');

                                    }
                                    else
                                    {
                                        ReplExecute(request, evalReq.expression);
                                        break;
                                    }
                                }

                                RespondError(request, "");
                            }
                        }
                        break;
                    default:
                        if (request != null)
                        {
                            RespondError(request, "unknown request");
                        }
                        break;
                }
            }
            else
            {
                Logger.Log("WARNING: IDE sent a non-request request! Ignoring...");
            }
        }

        private static void ReplExecute(RequestMessage request, string expression)
        {
            if (debugger == null) return;
            StringBuilder replSourceFile = new();
            var scope = debugInformation?.ScopeFromAddress(nextInstruction);
            if (scope != null)
            {
                var vars = debugInformation?.SymbolsInScope(scope);
                if (vars != null)
                {
                    List<DbgFile.SymbolInfo> symbols = new();
                    foreach (var v in vars)
                    {
                        if (symbols.Any(a => a.ID == v.ID)) continue;
                        int insert_idx = symbols.Count;
                        if (v.parent_symbol_id >= 0)
                        {
                            var parent = debugInformation?.Symbols?.Where(a => a.Value.ID == v.parent_symbol_id)?.Select(s => s.Value)?.FirstOrDefault();
                            if (parent != null)
                            {
                                if (!symbols.Any(a => a.ID == v.parent_symbol_id))
                                {
                                    symbols.Add(parent);
                                    insert_idx = symbols.Count;
                                }
                                else
                                {
                                    insert_idx = symbols.IndexOf(parent) + 1;
                                }
                            }
                        }
                        symbols.Insert(insert_idx, v);
                    }

                    foreach (var v in symbols)
                    {
                        replSourceFile.AppendLine($"{v.Name} := ${v.value:X4}");
                    }
                }
            }

            replSourceFile.AppendLine(".CODE");
            replSourceFile.AppendLine(expression);
            replSourceFile.AppendLine("BRK");

            string folder = Path.GetTempFileName();
            if (File.Exists(folder)) File.Delete(folder);
            Directory.CreateDirectory(folder);
            string asmName = Path.Combine(folder, "repl.asm");
            string cfgName = Path.Combine(folder, "repl.cfg");
            string objectName = Path.Combine(folder, "repl.o");
            string dbgname = Path.Combine(folder, "repl.dbg");
            string binName = Path.Combine(folder, "repl.bin");

            File.WriteAllText(asmName, replSourceFile.ToString());
            File.WriteAllText(cfgName, $$"""
                                            MEMORY {
                                                PROCSPACE: start = ${{debugger.ScratchLocation:X4}}, size = $10, file = %O;
                                            }
                                            SEGMENTS {
                                                CODE: load=PROCSPACE, type=rw;
                                            }
                                        """);

            if (RunProcess("CA65", $"{asmName} -g -o {objectName}") != 0)
            {
                RespondError(request, "compile error");
                return;
            }
            if (RunProcess("LD65", $"-C {cfgName} {objectName} --dbgfile {dbgname} -o {binName}") != 0)
            {
                RespondError(request, "linker error");
                return;
            }

            byte[] program = File.ReadAllBytes(binName);

            try
            {
                Directory.Delete(folder, true);
            }
            catch (Exception ex)
            {
                Logger.Log("error deleting folder");
                Logger.Log(ex);
            }

            if (program.Length == 0 || program.Length > 16)
            {
                RespondError(request, "invalid program");
                return;
            }

            byte opcode = program[0];
            byte[] disallowed_opcodes = new byte[]
            {
                0x0F,0x1F,0x2F,0x3F,0x4F,0x5F,0x6F,0x7F,0x8F,0x9F,0xAF,0xBF,0xCF,0xDF,0xEF,0xFF, // BBS / BBC
                0x00, 0x10, 0x20, 0x30, 0x40, 0x50, 0x60, 0x70, 0x80, 0x90, 0xB0, 0xD0, 0xF0, // regular branch, RTS, RTI, JSR
                0x4c, 0x6c, 0x7c  // JMP
            };

            if (disallowed_opcodes.Contains(opcode))
            {
                RespondError(request, "unsupported instruction");
                return;
            }

            debugger?.WriteMemory(debugger.ScratchLocation, program);
            debugger?.Execute(debugger.ScratchLocation);
            Respond(request, new
            {
                result = "OK.",
                variablesReference = 0,
            });
        }

        private static void Stopped(string reason, DapBreakpoint? hitBreakpoint = null)
        {
            int[]? bp_ids = null;
            if (hitBreakpoint != null) bp_ids = new int[] { hitBreakpoint.number };
            SendEvent(new
            {
                reason = reason,
                threadId = 1,
                allThreadsStopped = true,
                hitBreakpointIds = bp_ids
            }, "stopped");
        }

        private static void OnHalted(WDCMon.ExecutionHaltReason haltedWhy)
        {
            if (debugger == null) return;
            Logger.Log($"Execution halted at address ${debugger.BreakAt:X4}");
            nextInstruction = debugger.BreakAt;
            if (haltedWhy == WDCMon.ExecutionHaltReason.NMI)
            {
                Logger.Log("-> Halt pressed on device");
                Stopped("pause");
            }
            else
            {
                var currentBp = debugger?.CurrentBreakpoint();
                if (currentBp == null || currentBp.reference < 0)
                {
                    if (currentBp == null)
                    {
                        Logger.Log("-> halt not on Breakpoint?");
                    }
                    else
                    {
                        if (currentBp.breakpointType == Debugger.BreakpointType.TEMPORARY)
                        {
                            Logger.Log("-> halt on temporary bp");
                        }
                        else
                        {
                            Logger.Log($"-> halt on bp with reference {currentBp.reference}");
                        }
                    }
                    Stopped("step");
                }
                else
                {
                    Logger.Log($"-> halt on bp with reference {currentBp.reference}");
                    var hitBps = ConfiguredBreakpoints.Where(bp => bp.number == currentBp.reference).ToArray();
                    Stopped("breakpoint", hitBps.FirstOrDefault());
                }
            }
        }

        /// <summary>
        /// Add the following Breakpoints and update them
        /// Send an event if any of them change.
        /// </summary>
        /// <param name="toAdd"></param>
        private static void DebuggerAddBreakpoints(DapBreakpoint[] toAdd, bool events)
        {
            Logger.Log($"Adding {toAdd.Length} breakpoints to debugger.");
            foreach (var bp in toAdd)
            {
                if (bp == null) continue;
                int line = bp.line;
                if (adapterInfos?.linesStartAt1 == false) line++;
                var Line = debugInformation?.GetLine(bp?.source?.path ?? "", line);
                if (Line == null)
                {
                    if (bp != null)
                    {
                        bp.verified = false;
                        bp.message = "Source not Loaded / Line not found.";
                        if (events) SendEvent(new { reason = "changed", breakpoint = bp }, "breakpoint");
                    }
                }
                else
                {
                    if (debugInformation != null && debugger != null && bp != null && adapterInfos != null)
                    {
                        var address = debugInformation.AddressFromLine(Line);
                        bp.instructionReference = $"0x{address:X4}";
                        bp.verified = true;
                        bp.line = Line.line_number;
                        if (adapterInfos.linesStartAt1 == false) bp.line--;
                        bp.message = $"${address:X4}";
                        if (events) SendEvent(new { reason = "changed", breakpoint = bp }, "breakpoint");

                        debugger.CreateBreakpointAt(address, Debugger.BreakpointType.REGULAR, bp.number);
                    }
                }
            }
        }

        /// <summary>
        /// Remove Breakpoints from the system
        /// </summary>
        /// <param name="toRemove"></param>
        private static void DebuggerRemoveBreakpoints(DapBreakpoint[] toRemove, bool events)
        {
            Logger.Log($"Removing {toRemove.Length} breakpoints from debugger.");
            foreach (var bp in toRemove)
            {
                debugger?.RemoveBreakpointByReference(bp.number);
                if (events) SendEvent(new { reason = "removed", breakpoint = bp }, "breakpoint");
            }
        }

        private static string? SetVariableRequest(SetVariableRequest setVarReq)
        {
            if (setVarReq.variablesReference == registerScopeId && debugger != null)
            {
                switch (setVarReq.name?.ToUpper() ?? "")
                {
                    case "A":
                    case "X":
                    case "Y":
                    case "SP":
                        {
                            try
                            {
                                string val_s = setVarReq.value ?? "0";
                                byte value = 0;
                                val_s = val_s.Trim();
                                if (val_s.StartsWith("$"))
                                {
                                    val_s = val_s[1..];
                                    value = byte.Parse(val_s, System.Globalization.NumberStyles.HexNumber);
                                }
                                else if (val_s.ToLower().StartsWith("0x"))
                                {
                                    val_s = val_s[2..];
                                    value = byte.Parse(val_s, System.Globalization.NumberStyles.HexNumber);
                                }
                                else
                                {
                                    value = byte.Parse(val_s);
                                }

                                var state = debugger.GetProcessorState();
                                switch (setVarReq.name?.ToUpper() ?? "")
                                {
                                    case "A":
                                        state.A = value;
                                        break;
                                    case "X":
                                        state.X = value;
                                        break;
                                    case "Y":
                                        state.Y = value;
                                        break;
                                    case "SP":
                                        state.SP = value;
                                        break;
                                }
                                debugger.SetProcessorState(state);

                                return format8(value);
                            }
                            catch (Exception ex)
                            {
                                Logger.Log("error setting variable");
                                Logger.Log(ex);
                                return null;
                            }
                        }
                }
            }
            return null;
        }

        static string format8(byte b) => $"${b:X2}";
        static string format16(ushort s) => $"${s:X4}";
        static string formatStatus(byte status)
        {
            string binary = Convert.ToString(status, 2).PadLeft(8, '0');
            string?[] names = new string?[]
            {
                    "C", "Z", "I", "D", null, null, "V", "N"
            };
            return $"{binary[..2]}--{binary[4..]} ({String.Join(", ", Enumerable.Range(0, 8).Where(r => ((status & (1 << r)) > 0)).Select(s => names[s]).Where(s => s != null))})";
        }

        private static IEnumerable<string> VariableNamesInScope(int variablesReference)
        {
            if (variablesReference == registerScopeId)
            {
                yield return "A";
                yield return "X";
                yield return "Y";
                yield return "SP";
                yield return "STATUS";
                yield return "PC";
            }
        }
        private static Variable? GetVariable(int variablesReference, string name)
        {
            if (variablesReference == registerScopeId && debugger != null)
            {
                var procState = debugger.GetProcessorState();
                switch (name.ToUpper())
                {
                    case "A":
                        return new Variable() { name = "A", value = format8(procState.A) };
                    case "X":
                        return new Variable() { name = "X", value = format8(procState.X) };
                    case "Y":
                        return new Variable() { name = "Y", value = format8(procState.Y) };
                    case "SP":
                        return new Variable() { name = "SP", value = format8(procState.SP) };
                    case "STATUS":
                        return new Variable() { name = "STATUS", value = formatStatus(procState.Status) };
                    case "PC":
                        return new Variable() { name = "PC", value = format16(procState.PC) };
                }
            }
            return null;
        }

        private static Variable[] VariablesInScope(int scopeId) => VariableNamesInScope(scopeId).Select(v => GetVariable(scopeId, v)).Where(w => w != null).ToArray();

        public static void DebugConsole(string message) => SendOutputEvent(message + "\n", "debug console");
        public static void Stdout(string message) => SendOutputEvent(message, "stdout");
        public static void Stderr(string message) => SendOutputEvent(message, "stderr");

        public static int RunProcess(string executable, string args)
        {
            ProcessStartInfo psi = new(executable, args)
            {
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                StandardErrorEncoding = Encoding.UTF8,
                StandardOutputEncoding = Encoding.UTF8,
                UseShellExecute = false
            };
            var process = Process.Start(psi);
            if (process == null) return -1;
            var stdout = process.StandardOutput.ReadToEnd();
            var stderr = process.StandardError.ReadToEnd();
            if (!String.IsNullOrWhiteSpace(stdout))
            {
                var lines = stdout.Split('\n').Select(s => s.Trim('\r')).ToList();
                while (lines.Count > 0 && String.IsNullOrWhiteSpace(lines.Last()))
                {
                    lines.RemoveAt(lines.Count - 1);
                }
                foreach (var ln in lines)
                {
                    Stdout($"{executable}: {ln}\n");
                }
            }
            if (!String.IsNullOrWhiteSpace(stderr))
            {
                var lines = stderr.Split('\n').Select(s => s.Trim('\r')).ToList();
                while (lines.Count > 0 && String.IsNullOrWhiteSpace(lines.Last()))
                {
                    lines.RemoveAt(lines.Count - 1);
                }
                foreach (var ln in lines)
                {
                    Stderr($"{executable}: {ln}\n");
                }
            }
            process.WaitForExit();
            return process.ExitCode;
        }

        private static void SendOutputEvent(string message, string category)
        {
            SendEvent(new
            {
                category,
                output = message
            }, "output");
        }
    }
}