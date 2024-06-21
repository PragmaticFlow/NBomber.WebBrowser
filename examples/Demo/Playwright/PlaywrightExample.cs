using NBomber.CSharp;
using NBomber.WebBrowser.Playwright;

namespace Demo.Playwright;

using Microsoft.Playwright;

public class PlaywrightExample
{
    public static async Task Run()
    {
        var browserPath = "C:/Program Files/Google/Chrome/Application/chrome.exe";
        
        using var playwright = await Playwright.CreateAsync();
        
        await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { ExecutablePath = browserPath });

        var scenario = Scenario.Create("playwright_scenario", async context =>
        {
            var page = await browser.NewPageAsync();
            var pageResponse = await page.GotoAsync("https://nbomber.com");
            
            var response = await pageResponse.ToNBomberResponse();
            return response;
        })
        .WithoutWarmUp()
        .WithLoadSimulations(
            Simulation.KeepConstant(1, TimeSpan.FromMinutes(1))
        );

        NBomberRunner
            .RegisterScenarios(scenario)
            .Run();
    }
}