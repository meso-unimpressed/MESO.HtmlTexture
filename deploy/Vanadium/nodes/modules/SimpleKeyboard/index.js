let Keyboard = window.SimpleKeyboard.default;

let shiftPressed = false;

let commonKeyboardOptions = {
    onChange: input => onChange(input),
    onKeyPress: button => onKeyPress(button),
    syncInstanceInputs: true,
    mergeDisplay: true,
    newLineOnEnter: true,
    tabCharOnTab: true
};

function onChange(input) {
    if ("vvvvKb" in window) {
        vvvvKb.onChange(input);
    }
}

function clearInput()
{
    mainKeyboard.clearInput();
    if ("vvvvKb" in window) {
        vvvvKb.onChange("");
    }
}

function handleShift() {
    let currentLayout = mainKeyboard.options.layoutName;
    let shiftToggle = currentLayout === "default" ? "shift" : "default";

    mainKeyboard.setOptions({
        layoutName: shiftToggle
    });
}

function onKeyPress(button) {
    if(shiftPressed) {
        mainKeyboard.setOptions({
            layoutName: "default"
        });
        shiftPressed = false;
    }

    if (
      button === "{shift}" ||
      button === "{shiftleft}" ||
      button === "{shiftright}"
    ) {
        shiftPressed = true;
        handleShift();
    }
    if(button === "{capslock}") handleShift();
    if ("vvvvKb" in window) {
        vvvvKb.onKeyPress(button);
    }
}

let mainKeyboard = new Keyboard(".simple-keyboard", {
    ...commonKeyboardOptions,
    layout: {
        default: [
            "{escape} 1 2 3 4 5 6 7 8 9 0 {backspace}",
            "{tab} q w e r t y u i o p ?",
            "{capslock} a s d f g h j k l {enter}",
            "{shiftleft} z x c v b n m , . / ( )",
            ".com @ {space}"
        ],
        shift: [
            "{escape} ! \" * $ % & € + - = {backspace}",
            "{tab} Q W E R T Y U I O P ÷",
            "{capslock} A S D F G H J K L {enter}",
            "{shiftleft} Z X C V B N M ; : \\ [ ]",
            ".org .net {space}"
        ]
    }
});
