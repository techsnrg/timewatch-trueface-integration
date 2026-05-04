using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TrueFaceConnector;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);
builder.Services.AddWindowsService(options => options.ServiceName = "TrueFace ERPNext Connector");
builder.Services.Configure<ConnectorOptions>(builder.Configuration.GetSection("Connector"));
builder.Services.AddSingleton<PunchQueue>();
builder.Services.AddHttpClient<ErpNextClient>();
builder.Services.AddSingleton<ITrueFaceSdkClientFactory, TrueFaceSdkClientFactory>();
builder.Services.AddHostedService<Worker>();

IHost host = builder.Build();
await host.RunAsync();
