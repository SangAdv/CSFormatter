import * as vscode from 'vscode';
import { execFile } from 'child_process';
import * as path from 'path';
import * as fs from 'fs';

export function activate(context: vscode.ExtensionContext): void {
  context.subscriptions.push(
    vscode.commands.registerCommand('csformatter.format', () => formatActiveDocument(context))
  );
}

async function formatActiveDocument(context: vscode.ExtensionContext): Promise<void> {
  const editor = vscode.window.activeTextEditor;

  if (!editor || editor.document.languageId !== 'csharp') {
    void vscode.window.showWarningMessage('CSFormatter: open a C# file first.');
    return;
  }

  const cli = resolveCli(context);
  if (!cli) {
    void vscode.window.showErrorMessage(
      `CSFormatter: formatter executable not found at ${path.join(context.extensionPath, 'bin')}. ` +
        'Run "npm run build:cli" in the extension folder.'
    );
    return;
  }

  const document = editor.document;
  const original = document.getText();

  let formatted: string;
  try {
    formatted = await runCli(cli, { text: original, options: readOptions(editor) });
  } catch (error) {
    const message = error instanceof Error ? error.message : String(error);
    void vscode.window.showErrorMessage(`CSFormatter failed: ${message}`);
    return;
  }

  if (formatted === original) {
    vscode.window.setStatusBarMessage('CSFormatter: already formatted', 3000);
    return;
  }

  const edit = new vscode.WorkspaceEdit();
  edit.replace(
    document.uri,
    new vscode.Range(document.positionAt(0), document.positionAt(original.length)),
    formatted
  );

  if (!(await vscode.workspace.applyEdit(edit))) {
    void vscode.window.showErrorMessage('CSFormatter: could not apply the formatted text.');
  }
}

function readOptions(editor: vscode.TextEditor): Record<string, unknown> {
  const config = vscode.workspace.getConfiguration('csformatter', editor.document.uri);

  // Indentation follows the editor's own settings for this file.
  const tabSize = editor.options.tabSize;
  const indentSize = typeof tabSize === 'number' ? tabSize : 4;

  return {
    indentSize,
    useTabs: editor.options.insertSpaces === false,
    formatComments: config.get<boolean>('formatComments', true),
    commentWrapColumn: config.get<number>('commentWrapColumn', 100),
    sortUsings: config.get<boolean>('sortUsings', true),
    padMembers: config.get<boolean>('padMembers', true),
  };
}

function resolveCli(context: vscode.ExtensionContext): string | undefined {
  const candidates =
    process.platform === 'win32'
      ? ['csformatter.exe']
      : ['csformatter', 'csformatter.exe'];

  for (const name of candidates) {
    const full = path.join(context.extensionPath, 'bin', name);
    if (fs.existsSync(full)) {
      return full;
    }
  }

  return undefined;
}

interface CliResponse {
  text?: string;
  error?: string;
}

function runCli(cli: string, request: unknown): Promise<string> {
  return new Promise((resolve, reject) => {
    const child = execFile(
      cli,
      [],
      { encoding: 'utf8', maxBuffer: 64 * 1024 * 1024, windowsHide: true },
      (error, stdout, stderr) => {
        let response: CliResponse | undefined;
        try {
          response = JSON.parse(stdout) as CliResponse;
        } catch {
          // Not JSON, fall through to the process error below.
        }

        if (response?.error) {
          reject(new Error(response.error));
          return;
        }

        if (typeof response?.text === 'string') {
          resolve(response.text);
          return;
        }

        reject(new Error(stderr?.trim() || error?.message || 'CSFormatter returned no output.'));
      }
    );

    child.stdin?.end(JSON.stringify(request), 'utf8');
  });
}
