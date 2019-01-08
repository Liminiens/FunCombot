const path = require("path");
const MiniCssExtractPlugin = require("mini-css-extract-plugin");
const ExtraneousFileCleanupPlugin = require('webpack-extraneous-file-cleanup-plugin');
const UglifyJsPlugin = require('uglifyjs-webpack-plugin');

const outputPath = path.join(__dirname, '/wwwroot');
console.log('Output: ' + outputPath);

const scriptsDir =  path.join(outputPath, '/scripts');

module.exports = {
    mode: "development",
    entry: {
        scripts: './frontend/scripts/index.ts',
        styles: './frontend/styles/index.scss'
    },
    output: {
        filename: '[name].js',
        path: scriptsDir,
        library: 'statsbot',
        libraryTarget: 'window'
    },
    devtool: 'inline-source-map',
    module: {
        rules: [
            {
                test: /\.tsx?$/,
                use: 'ts-loader',
                exclude: /node_modules/
            },
            {
                test: /\.scss$/,
                use: [
                    {
                        loader: MiniCssExtractPlugin.loader
                    },
                    {
                        loader: "css-loader",
                        options: {
                            sourceMap: true
                        }
                    },
                    {
                        loader: "postcss-loader",
                        options: {
                            sourceMap: true
                        }
                    },
                    {
                        loader: "sass-loader",
                        options: {
                            sourceMap: true
                        }
                    }
                ]
            }
        ]
    },
    optimization: {
        removeEmptyChunks: true,
        minimizer: [new UglifyJsPlugin()]
    },
    resolve: {
        extensions: ['.tsx', '.ts', '.js', '.scss']
    },
    externals: {
        "c3": "c3",
        "jquery": "jQuery"
    },
    plugins: [
        new MiniCssExtractPlugin({
            filename: "[name].css",
            chunkFilename: "[id].css"
        }),
        new ExtraneousFileCleanupPlugin({
            extensions: ['.js']
        })
    ]
};