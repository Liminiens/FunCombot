import * as c3 from "c3";
import {ChartConfiguration} from "c3";

interface IColumnData  {
    name: string;
    data: Array<string | boolean | number | null>
}

interface IAxis  {
    type: string,
    tick: {
        format: string
    }
}

interface IAxisConfiguration {
    x: IAxis
}

interface IChartConfiguration {
    x?: string,
    columns: Array<IColumnData>,
    axis?: IAxisConfiguration
}

function convertToC3(bindTo: string, configuration: IChartConfiguration): ChartConfiguration {
    const result = {
        bindto: bindTo,
        data: {
            x: configuration.x,
            columns: configuration.columns.map(c => [c.name, ...c.data])
        },
        axis: configuration.axis
    };
    return result;
}

export function drawChart(bindTo: string, data: IChartConfiguration): void {
    c3.generate(convertToC3(bindTo, data));
}