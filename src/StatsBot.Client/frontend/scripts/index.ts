import * as charting from "./charting/index";
import * as $ from "jquery";

export {charting};

export function initDropdowns(): void {
    /// #if DEBUG
    console.log("Initialized semantic dropdowns");
    /// #endif
    $('.ui.dropdown').dropdown();
}