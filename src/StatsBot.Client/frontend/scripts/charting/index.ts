import * as c3 from "c3";
import {ChartConfiguration, ChartAPI} from "c3";

interface IColumnData {
    name: string;
    data: Array<string | boolean | number | null>
}

interface IAxis {
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

type guid = string

const chartCache = new Map<guid, ChartAPI>();

function convertToC3(bindTo: string, configuration: IChartConfiguration): ChartConfiguration {
    return {
        bindto: bindTo,
        data: {
            x: configuration.x,
            columns: configuration.columns.map(c => [c.name, ...c.data])
        },
        axis: configuration.axis,
        grid: {
            x: {
                show: true
            },
            y: {
                show: true
            }
        }
    };
}

export function drawChart(guid: guid, bindTo: string, data: IChartConfiguration): void {
    chartCache.set(guid, c3.generate(convertToC3(bindTo, data)));
}

export function loadData(guid: guid, data: Array<IColumnData>): void {
    const chart = chartCache.get(guid);
    if (chart !== undefined) {
        chart.load({
            columns: data.map(c => [c.name, ...c.data])
        });
    } else {
        console.error(`Attempt to load data to a chart that doesn't exist in a cache: ${guid}`);
    }
}

export function unloadData(guid: guid, columnNames: Array<string>): void {
    const chart = chartCache.get(guid);
    if (chart !== undefined) {
        chart.unload({
            ids: columnNames
        });
    } else {
        console.error(`Attempt to unload data from a chart that doesn't exist in a cache: ${guid}`);
    }
}

export function destroyChart(guid: guid): void {
    const chart = chartCache.get(guid);
    if (chart !== undefined) {
        chart.destroy();
        chartCache.delete(guid);
    }
}