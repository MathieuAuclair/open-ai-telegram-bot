using BotDashboard.Services;

class Program
{
    static async Task Main(string[] args)
    {
        var host = Host.CreateDefaultBuilder(args)
            .ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder.ConfigureServices((context, services) =>
                {
                    services.AddHostedService<BotService>();
                    services.AddControllersWithViews();
                });

                webBuilder.Configure(app =>
                {
                    var env = app.ApplicationServices.GetRequiredService<IHostEnvironment>();
                    if (env.IsDevelopment())
                    {
                        app.UseDeveloperExceptionPage();
                    }

                    app.UseRouting();
                    app.UseEndpoints(endpoints =>
                    {
                        endpoints.MapControllers();
                    });
                });

                webBuilder.UseUrls("http://0.0.0.0:80", "https://0.0.0.0:443");
            })
            .Build();

        await host.RunAsync();
    }
}