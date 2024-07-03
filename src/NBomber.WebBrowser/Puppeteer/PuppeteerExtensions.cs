using PuppeteerSharp;

namespace NBomber.WebBrowser.Puppeteer;

public static class PuppeteerExtensions
{
    public static Task<string> GetDataTransferResources(this IPage page)
    {
        return page.EvaluateFunctionAsync<string>(
            """
            () =>
            {
            	return JSON.stringify(performance.getEntries());
            }
            """);
    }

    public static Task<long> GetDataTransferSize(this IPage page)
    {
        return page.EvaluateFunctionAsync<long>(
            """
            () =>
            {
            	var totalSize = 0;
            
            	performance.
            		getEntries()
            		.forEach((entry) =>
            		{
            			if (entry.transferSize > 0) {
            	            totalSize += entry.transferSize;
            	        }
            		});
            
            	return totalSize;
            }
            """);
    }
}