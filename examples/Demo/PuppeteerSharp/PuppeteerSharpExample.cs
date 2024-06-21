using NBomber.CSharp;
using NBomber.WebBrowser.PuppeteerSharp;
using PuppeteerSharp;

namespace Demo.PuppeteerSharp;

public class PuppeteerSharpExample
{
    public static async Task Run()
    {
        var browserPath = "C:/Program Files/Google/Chrome/Application/chrome.exe";
        
        await using var browser = await Puppeteer.LaunchAsync(new LaunchOptions { Headless = true, ExecutablePath = browserPath });

        var scenario = Scenario.Create("puppeteer_scenario", async context =>
        {
            await using var page = await browser.NewPageAsync();
            var pageResponse = await page.GoToAsync("https://translate.google.com/");

            var response = await pageResponse.ToNBomberResponse();
            page.CloseAsync();
            
            return response;
        })
        .WithoutWarmUp()
        .WithLoadSimulations(
            Simulation.KeepConstant(1, TimeSpan.FromSeconds(60))
        );

        NBomberRunner
            .RegisterScenarios(scenario)
            .Run();
    }
}