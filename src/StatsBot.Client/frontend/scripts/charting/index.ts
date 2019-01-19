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
    /// #if DEBUG
    console.log(`${nameof(drawChart)} called with arguments: `);
    console.log(arguments);
    /// #endif
    chartCache.set(guid, c3.generate(convertToC3(bindTo, data)));
}

export function loadData(guid: guid, data: Array<IColumnData>): void {
    /// #if DEBUG
    console.log(`${nameof(loadData)} called with arguments: `);
    console.log(arguments);
    /// #endif
    const chart = chartCache.get(guid);
    if (chart !== undefined) {
        /// #if DEBUG
        console.log(`C3 chart object:`);
        console.log(chart);
        /// #endif
        chart.load({
            columns: data.map(c => [c.name, ...c.data])
        });
        console.log('Loaded data for chart');
    } else {
        console.error(`Attempt to load data to a chart that doesn't exist in a cache: ${guid}`);
    }
}

export function unloadData(guid: guid, columnNames: Array<string>): void {
    const chart = chartCache.get(guid);
    /// #if DEBUG
    console.log(`${nameof(unloadData)} called with arguments: `);
    console.log(arguments);
    /// #endif
    if (chart !== undefined) {
        /// #if DEBUG
        console.log(`Started chart unloading ${guid}`);
        /// #endif
        chart.unload({
            ids: columnNames
        });
        /// #if DEBUG
        console.log(`Unloaded chart ${guid}`);
        /// #endif
    } else {
        console.error(`Attempt to unload data from a chart that doesn't exist in a cache: ${guid}`);
    }
}

export function destroyChart(guid: guid): void {
    /// #if DEBUG
    console.log(`${nameof(destroyChart)} called with arguments: `);
    console.log(arguments);
    /// #endif
    const chart = chartCache.get(guid);
    if (chart !== undefined) {
        /// #if DEBUG
        console.log('C3 chart to destroy: ');
        console.log(chart);
        /// #endif
        chart.destroy();
        chartCache.delete(guid);
        /// #if DEBUG
        console.log('Destroyed chart');
        /// #endif
    }
}