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
                    // Register your bot as a background hosted service
                    services.AddHostedService<BotService>();

                    // Register controllers (for your /redirect endpoint etc.)
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

                // Listen on both 80 and 443 (make sure you have permissions or run as root if required)
                webBuilder.UseUrls("http://0.0.0.0:80", "https://0.0.0.0:443");
            })
            .Build();

        await host.RunAsync();
    }
}