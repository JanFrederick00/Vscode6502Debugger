// The module 'vscode' contains the VS Code extensibility API
// Import the module and reference it with the alias vscode in your code below
import * as vscode from 'vscode';

class MyDebugConfigurationProvider {

	public provideDebugConfiguration(folder: vscode.WorkspaceFolder, token?: vscode.CancellationToken) : vscode.ProviderResult<vscode.DebugConfiguration[]>
	{ 
		return [];
	}

	public resolveDebugConfiguration(folder: vscode.WorkspaceFolder, debugConfiguration : vscode.DebugConfiguration, token?: vscode.CancellationToken) : vscode.ProviderResult<vscode.DebugConfiguration> 
	{
		return debugConfiguration;
	}

	public resolveDebugConfigurationWithSubstitutedVariables(folder: vscode.WorkspaceFolder, debugConfiguration: vscode.DebugConfiguration, token?: vscode.CancellationToken) : vscode.ProviderResult<vscode.DebugConfiguration> 
	{
		return this.resolveDebugConfiguration(folder, debugConfiguration, token);
	}

}

// This method is called when your extension is activated
// Your extension is activated the very first time the command is executed
export function activate(context: vscode.ExtensionContext) {

	// Use the console to output diagnostic information (console.log) and errors (console.error)
	// This line of code will only be executed once when your extension is activated
	console.log('Congratulations, your extension "wdcmon-debug" is now active!');

	// The command has been defined in the package.json file
	// Now provide the implementation of the command with registerCommand
	// The commandId parameter must match the command field in package.json
	let disposable = vscode.commands.registerCommand('wdcmon-debug.helloWorld', () => {
		// The code you place here will be executed every time your command is executed
		// Display a message box to the user
		vscode.window.showInformationMessage('Hello World from wdcmon-debug!');
	});

	//vscode.debug.registerDebugConfigurationProvider("wdcmon", new MyDebugConfigurationProvider());

	context.subscriptions.push(disposable);
}

// This method is called when your extension is deactivated
export function deactivate() {}
