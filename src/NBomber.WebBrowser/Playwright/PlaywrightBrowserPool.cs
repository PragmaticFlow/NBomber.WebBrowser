using Microsoft.Playwright;

namespace NBomber.WebBrowser.Playwright;

/// <summary>
/// Manages a pool of Playwright browser contexts for parallel load testing with NBomber.
/// Creates multiple Playwright instances and browsers to avoid CDP (Chrome DevTools Protocol) serialization bottleneck.
/// </summary>
/// <example>
/// <code>
/// await using var pool = new PlaywrightBrowserPool();
/// await pool.Initialize(virtualUsers: 100, browserPath: "/path/to/chrome");
/// var context = pool.GetBrowserContext(instanceNumber);
/// </code>
/// </example>
public class PlaywrightBrowserPool : IAsyncDisposable
{
    private readonly List<IPlaywright> _playwrightInstances = [];
    private readonly List<IBrowser> _browsers = [];
    private ClientPool<IBrowserContext>? _contextPool;

    /// <summary>
    /// Gets a browser context from the pool for the specified virtual user instance.
    /// </summary>
    /// <param name="instanceNumber">The virtual user instance number (from ScenarioInfo.InstanceNumber).</param>
    /// <returns>An isolated browser context for the virtual user.</returns>
    /// <exception cref="InvalidOperationException">Thrown when pool is not initialized.</exception>
    public IBrowserContext GetBrowserContext(int instanceNumber)
    {
        if (_contextPool == null)
            throw new InvalidOperationException("Pool not initialized. Call InitializeAsync first.");

        return _contextPool.GetClient(instanceNumber);
    }

    /// <summary>
    /// Initializes the browser pool with the specified configuration.
    /// Creates Playwright instances, browsers, and contexts based on the number of virtual users.
    /// Uses a 1:3 ratio of Playwright instances to browsers for optimal performance.
    /// </summary>
    /// <param name="virtualUsers">Number of concurrent virtual users to support.</param>
    /// <param name="browserPath">Path to the Chrome/Chromium executable.</param>
    /// <param name="isHeadless">Run browsers in headless mode. Default is true.</param>
    /// <param name="maxContextsPerBrowser">Maximum browser contexts per browser instance. Default is 50.</param>
    public async Task Initialize(
        int virtualUsers,
        string browserPath,
        bool isHeadless = true,
        int maxContextsPerBrowser = 50)
    {
        // Clear previous instances if any
        _playwrightInstances.Clear();
        _browsers.Clear();

        var contextPool = new ClientPool<IBrowserContext>();

        var (playwrightInstanceCount, browsersPerInstance, contextsPerBrowser) = CalculateInstancesCount(virtualUsers, maxContextsPerBrowser);

        _contextPool = contextPool;

        for (var i = 0; i < playwrightInstanceCount; i++)
        {
            var playwright = await Microsoft.Playwright.Playwright.CreateAsync();
            _playwrightInstances.Add(playwright);

            for (var b = 0; b < browsersPerInstance; b++)
            {
                var browser = await playwright.Chromium.LaunchAsync(
                    new BrowserTypeLaunchOptions
                    {
                        Headless = isHeadless,
                        ExecutablePath = browserPath,
                        Args = new[]
                        {
                            "--no-sandbox",                    // Disables Chrome sandbox; required in Docker/CI, speeds up startup
                            "--disable-setuid-sandbox",        // Disables SUID sandbox (Linux only)
                            "--disable-dev-shm-usage",         // Uses /tmp instead of /dev/shm; critical for high load in containers
                            "--disable-gpu",                   // Disables GPU acceleration; not needed for load testing
                            "--disable-extensions"             // Disables extensions; reduces per-browser overhead
                        }
                    }
                );
                _browsers.Add(browser);

                for (var j = 0; j < contextsPerBrowser; j++)
                {
                    var browserContext = await browser.NewContextAsync();
                    contextPool.AddClient(browserContext);
                }
            }
        }
    }

    /// <summary>
    /// Disposes all browser contexts, browsers, and Playwright instances.
    /// Called automatically when using 'await using' pattern.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        if (_contextPool != null)
        {
            foreach (var browserContext in _contextPool.Clients)
            {
                try
                {
                    await browserContext.CloseAsync();
                }
                catch
                {
                    // Context may already be closed if browser was closed
                }
            }
        }

        foreach (var browser in _browsers)
        {
            try
            {
                await browser.CloseAsync();
                await browser.DisposeAsync();
            }
            catch
            {
                // Browser may already be closed
            }
        }

        foreach (var playwright in _playwrightInstances)
        {
            playwright.Dispose();
        }

        _browsers.Clear();
        _playwrightInstances.Clear();
        _contextPool = null;
    }

    private (int playwrightInstanceCount, int browsersPerInstance, int contextsPerBrowser) CalculateInstancesCount(
        int virtualUsers,
        int maxContextsPerBrowser)
    {
        if (virtualUsers <= 0)
            throw new ArgumentException("Virtual users must be greater than 0", nameof(virtualUsers));

        // Step 1: Calculate total browsers needed based on contexts per browser
        var totalBrowsersNeeded = Math.Max(1, (int)Math.Ceiling((double)virtualUsers / maxContextsPerBrowser));

        // Step 2: Calculate Playwright instances based on browsers (optimal ratio 1:3)
        var playwrightInstanceCount = CalculateOptimalPlaywrightInstanceCount(totalBrowsersNeeded);

        // Step 3: Distribute browsers evenly across Playwright instances
        var browsersPerInstance = Math.Max(1, (int)Math.Ceiling((double)totalBrowsersNeeded / playwrightInstanceCount));

        // Step 4: Calculate contexts per browser
        var totalBrowsers = playwrightInstanceCount * browsersPerInstance;
        var contextsPerBrowser = (int)Math.Ceiling((double)virtualUsers / totalBrowsers);

        return (playwrightInstanceCount, browsersPerInstance, contextsPerBrowser);
    }

    private int CalculateOptimalPlaywrightInstanceCount(int browsersNeeded)
    {
        // Strategy: Each Playwright instance manages 2-4 browsers
        const int browsersPerInstance = 3;

        var instancesNeeded = (int)Math.Ceiling((double)browsersNeeded / browsersPerInstance);

        // Cap at 6 instances max (diminishing returns beyond this)
        return Math.Min(6, Math.Max(1, instancesNeeded));
    }
}
