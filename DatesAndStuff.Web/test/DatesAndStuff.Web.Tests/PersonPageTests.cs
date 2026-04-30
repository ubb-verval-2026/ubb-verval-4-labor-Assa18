using System;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using FluentAssertions;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using SeleniumExtras.WaitHelpers;

namespace DatesAndStuff.Web.Tests;

[TestFixture]
public class PersonPageTests
{
    private IWebDriver driver;
    private StringBuilder verificationErrors;
    private const string BaseURL = "http://localhost:5091";
    private bool acceptNextAlert = true;

    private Process? _blazorProcess;

    [OneTimeSetUp]
    public void StartBlazorServer()
    {
        var webProjectPath = Path.GetFullPath(Path.Combine(
            Assembly.GetExecutingAssembly().Location,
            "../../../../../../src/DatesAndStuff.Web/DatesAndStuff.Web.csproj"
            ));

        var webProjFolderPath = Path.GetDirectoryName(webProjectPath);

        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            //Arguments = $"run --project \"{webProjectPath}\"",
            Arguments = "dotnet run --no-build",
            WorkingDirectory = webProjFolderPath,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };

        _blazorProcess = Process.Start(startInfo);

        // Wait for the app to become available
        var client = new HttpClient();
        var timeout = TimeSpan.FromSeconds(30);
        var start = DateTime.Now;

        while (DateTime.Now - start < timeout)
        {
            try
            {
                var result = client.GetAsync(BaseURL).Result;
                if (result.IsSuccessStatusCode)
                {
                    break;
                }
            }
            catch (Exception e)
            {
                Thread.Sleep(1000);
            }
        }
    }

    [OneTimeTearDown]
    public void StopBlazorServer()
    {
        if (_blazorProcess != null && !_blazorProcess.HasExited)
        {
            _blazorProcess.Kill(true);
            _blazorProcess.Dispose();
        }
    }

    [SetUp]
    public void SetupTest()
    {
        driver = new ChromeDriver();
        verificationErrors = new StringBuilder();
    }

    [TearDown]
    public void TeardownTest()
    {
        try
        {
            driver.Quit();
            driver.Dispose();
        }
        catch (Exception)
        {
            // Ignore errors if unable to close the browser
        }
        Assert.That(verificationErrors.ToString(), Is.EqualTo(""));
    }

    [Test]
    [TestCase(0)]
    [TestCase(1)]
    [TestCase(5)]
    [TestCase(100)]
    public void Person_SalaryIncrease_ShouldIncrease(double value)
    {
        // Arrange
        driver.Navigate().GoToUrl(BaseURL);
        driver.FindElement(By.XPath("//*[@data-test='PersonPageNavigation']")).Click();

        var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(5));

        var input = wait.Until(ExpectedConditions.ElementExists(By.XPath("//*[@data-test='SalaryIncreasePercentageInput']")));
        var salaryLabel = wait.Until(ExpectedConditions.ElementExists(By.XPath("//*[@data-test='DisplayedSalary']")));
        double actualSalary = double.Parse(salaryLabel.Text);
        var expectedSalary = actualSalary + value / 100 * actualSalary;
        input.Clear();
        input.SendKeys(value.ToString());

        // Act
        var submitButton = wait.Until(ExpectedConditions.ElementExists(By.XPath("//*[@data-test='SalaryIncreaseSubmitButton']")));
        submitButton.Click();


        // Assert
        var salaryAfterSubmission = double.Parse(salaryLabel.Text);
        salaryAfterSubmission.Should().BeApproximately(expectedSalary, 0.001);
    }

    [Test]
    public void Person_Negative_Percent_Errors_Shown()
    {
        driver.Navigate().GoToUrl(BaseURL);
        driver.FindElement(By.XPath("//*[@data-test='PersonPageNavigation']")).Click();

        var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(5));
        
        var input = wait.Until(ExpectedConditions.ElementExists(By.XPath("//*[@data-test='SalaryIncreasePercentageInput']")));

        input.Clear();
        input.SendKeys("-11");
        
        var submitButton = wait.Until(ExpectedConditions.ElementExists(By.XPath("//*[@data-test='SalaryIncreaseSubmitButton']")));
        submitButton.Click();

        var errorMessageTop = driver.FindElement(By.ClassName("validation-errors"));
        var errorMessageBottom = driver.FindElement(By.ClassName("validation-message"));

        errorMessageTop.Should().NotBeNull();
        errorMessageBottom.Should().NotBeNull();
    }

    [Test]
    public void Flights_MexicoCity_To_Dublin_ShouldHaveAtLeast3Results()
    {
        driver.Navigate().GoToUrl("https://blazedemo.com/");
        
        var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));

        var fromPortElement = wait.Until(ExpectedConditions.ElementIsVisible(By.Name("fromPort")));

        var toPortElement = wait.Until(ExpectedConditions.ElementIsVisible(By.Name("toPort")));
        
        
        var fromPort = new SelectElement(fromPortElement);
        var toPort = new SelectElement(toPortElement);

        fromPort.SelectByText("Mexico City");
        toPort.SelectByText("Dublin");
        
        var findFlightsButton = wait.Until(ExpectedConditions.ElementExists(By.CssSelector("input[type='submit']")));
        
        findFlightsButton.Click();
        
        wait.Until(SeleniumExtras.WaitHelpers.ExpectedConditions.ElementIsVisible(
            By.XPath("//h3[contains(text(),'Flights from Mexico City to Dublin')]")));

        var rows = wait.Until(driver =>
        {
            var elements = driver.FindElements(By.CssSelector("table.table tbody tr"));
            return elements.Count >= 3 ? elements : null;
        });

        rows.Count.Should().BeGreaterThanOrEqualTo(3);
    }
    
    private bool IsElementPresent(By by)
    {
        try
        {
            driver.FindElement(by);
            return true;
        }
        catch (NoSuchElementException)
        {
            return false;
        }
    }

    private bool IsAlertPresent()
    {
        try
        {
            driver.SwitchTo().Alert();
            return true;
        }
        catch (NoAlertPresentException)
        {
            return false;
        }
    }

    private string CloseAlertAndGetItsText()
    {
        try
        {
            IAlert alert = driver.SwitchTo().Alert();
            string alertText = alert.Text;
            if (acceptNextAlert)
            {
                alert.Accept();
            }
            else
            {
                alert.Dismiss();
            }
            return alertText;
        }
        finally
        {
            acceptNextAlert = true;
        }
    }
}