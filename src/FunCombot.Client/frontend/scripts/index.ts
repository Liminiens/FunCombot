import * as charting from "./charting/index";
import * as $ from "jquery";

export {charting};

export function initDropdowns(): void {
    $('.ui.dropdown').dropdown();
}