
function installGlobalInteractions(listener) {
    console.info('app install');

    Mousetrap.bind('esc', () => listener.invokeMethodAsync('HandleEscape'));

    Mousetrap.bind('space', () => listener.invokeMethodAsync('HandleSpace'));
}
