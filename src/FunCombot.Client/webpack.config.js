module.exports = {
    entry: {
        index: './frontend/index.js'
    },
    output: {
        filename: '[name].js',
        path: __dirname + '/wwwroot/scripts',
        library: 'funcombot',
        libraryTarget: 'window'
    }
};