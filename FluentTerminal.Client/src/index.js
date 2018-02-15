import * as Terminal from '../node_modules/xterm/dist/xterm';
import * as attach from '../node_modules/xterm/dist/addons/attach/attach';
import * as fit from '../node_modules/xterm/dist/addons/fit/fit';
import * as winptyCompat from '../node_modules/xterm/dist/addons/winptyCompat/winptyCompat';


Terminal.applyAddon(attach);
Terminal.applyAddon(fit);
Terminal.applyAddon(winptyCompat);

var term, socket;
var terminalContainer = document.getElementById('terminal-container');

function createTerminal(options, theme) {
  while (terminalContainer.children.length) {
    terminalContainer.removeChild(terminalContainer.children[0]);
  }

  theme = JSON.parse(theme);
  theme.background = 'transparent';

  options = JSON.parse(options);

  var terminalOptions = {
    fontFamily: options.fontFamily,
    fontSize: options.fontSize,
    cursorStyle: options.cursorStyle,
    cursorBlink: options.cursorBlink,
    bellStyle: options.bellStyle,
    allowTransparency: true,
    theme: theme
  };

  term = new Terminal(terminalOptions);

  window.term = term;

  term.on('resize', function (size) {
    terminalBridge.notifySizeChanged(term.cols, term.rows);
  });

  term.on('title', function (title) {
    terminalBridge.notifyTitleChanged(title);
  });

  term.open(terminalContainer);
  term.winptyCompatInit();
  term.fit();
  term.focus();

  var resizeTimeout;
  window.onresize = function () {
    clearTimeout(resizeTimeout);
    resizeTimeout = setTimeout(term.fit(), 500);
  }

  return JSON.stringify({
    rows: term.rows,
    columns: term.cols
  });
}

function attachTerminal() {
  term.attach(socket);
  term._initialized = true;
}

function connectToWebSocket(url) {
  socket = new WebSocket(url);
  socket.onopen = attachTerminal;
}

function changeTheme(theme) {
  theme = JSON.parse(theme);
  theme.background = 'transparent';

  term.setOption('theme', theme);
}

function changeOptions(options) {
  options = JSON.parse(options);

  term.setOption('bellStyle', options.bellStyle);
  term.setOption('cursorBlink', options.cursorBlink);
  term.setOption('cursorStyle', options.cursorStyle);
  term.setOption('fontFamily', options.fontFamily);
  term.setOption('fontSize', options.fontSize);
}

window.createTerminal = createTerminal;
window.connectToWebSocket = connectToWebSocket;
window.changeTheme = changeTheme;
window.changeOptions = changeOptions;