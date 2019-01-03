import * as c3 from "c3";
import {ChartConfiguration} from "c3";

type IColumnData = {
    name: string;
    data: Array<string | boolean | number | null>
}

type IAxis = {
    type: string,
    tick: {
        format: string
    }
}

type IAxisConfiguration = {
    x: IAxis
}

type IChartConfiguration = {
    x?: string,
    columns: Array<IColumnData>,
    axis?: IAxisConfiguration
}

function convertToC3(bindTo: string, configuration: IChartConfiguration): ChartConfiguration {
    console.log(configuration);
    const result = {
        bindto: bindTo,
        data: {
            x: configuration.x,
            columns: configuration.columns.map(c => [c.name, ...c.data])
        },
        axis: configuration.axis
    };
    console.log(result);
    return result;
}

export function drawChart(bindTo: string, data: IChartConfiguration): void {
    const chart = c3.generate(convertToC3(bindTo, data));
    chart.resize({width: 500, height: 300})
}