using NBomber.CSharp;
using NBomber.WebBrowser.Playwright;
using PuppeteerSharp;

namespace Demo.Playwright;

public class PlaywrightExample
{
    public static async Task Run()
    {
        var installedBrowser = await new BrowserFetcher(SupportedBrowser.Chrome).DownloadAsync(BrowserTag.Stable);
        var browserPath = installedBrowser.GetExecutablePath();

        await using var playwrightBrowserPool = new PlaywrightBrowserPool();

        // Set the target number of concurrent virtual users
        var targetVirtualUsers = 3;

        var scenario = Scenario.Create("playwright_scenario", async context =>
        {
            // Get a browser context from the pool
            // This ensures each virtual user has its own isolated context
            var browserContext = playwrightBrowserPool.GetBrowserContext(context.ScenarioInfo.InstanceNumber);
            var page = await browserContext.NewPageAsync();

            try
            {
                await Step.Run("open local website", context, async () =>
                {
                    await page.GotoAsync("http://localhost:5280");
                    return Response.Ok();
                });

                return Response.Ok();
            }
            finally
            {
                // Ensure page is closed even if there's an exception
                // The browser context remains alive and will be reused
                await page.CloseAsync();
            }
        })
        .WithInit(async context => await playwrightBrowserPool.Initialize(targetVirtualUsers, browserPath))
        .WithWarmUpDuration(TimeSpan.FromSeconds(5))
        .WithLoadSimulations(
            Simulation.KeepConstant(targetVirtualUsers, TimeSpan.FromSeconds(30))
        );

        NBomberRunner
            .RegisterScenarios(scenario)
            .Run();
    }
}