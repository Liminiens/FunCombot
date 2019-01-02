import * as c3 from "c3";

type IData = {
    columns: Array<Array<string | boolean | number | null>>
}

type IChartConfiguration = {
    bindto: string;
    data: IData;
}

export function drawChart(data: IChartConfiguration): void {
    const chart = c3.generate(data);
    chart.resize({height:200, width:300})
}