using PleOps.Explitwiser.Console;
using Spectre.Console.Cli;

var app = new CommandApp();
app.Configure(config => {
#if DEBUG
    config.PropagateExceptions();
    config.ValidateExamples();
#endif

    config.AddCommand<ExportAllCommand>("export-all");
});
return await app.RunAsync(args);
