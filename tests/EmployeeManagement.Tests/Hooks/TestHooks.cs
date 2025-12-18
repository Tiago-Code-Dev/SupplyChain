using TechTalk.SpecFlow;

namespace EmployeeManagement.Tests.Hooks;

[Binding]
public class TestHooks
{
    [BeforeTestRun]
    public static void BeforeTestRun()
    {
        Console.WriteLine("========================================");
        Console.WriteLine("Iniciando execução dos testes BDD...");
        Console.WriteLine("========================================");
    }

    [AfterTestRun]
    public static void AfterTestRun()
    {
        Console.WriteLine("========================================");
        Console.WriteLine("Finalizando execução dos testes BDD...");
        Console.WriteLine("========================================");
    }

    [BeforeScenario]
    public void BeforeScenario(ScenarioContext scenarioContext)
    {
        Console.WriteLine($"Iniciando cenário: {scenarioContext.ScenarioInfo.Title}");
    }

    [AfterScenario]
    public void AfterScenario(ScenarioContext scenarioContext)
    {
        if (scenarioContext.TestError != null)
        {
            Console.WriteLine($"Cenário falhou: {scenarioContext.TestError.Message}");
        }
        else
        {
            Console.WriteLine($"Cenário concluído: {scenarioContext.ScenarioInfo.Title}");
        }
    }

    [BeforeFeature]
    public static void BeforeFeature(FeatureContext featureContext)
    {
        Console.WriteLine($"Iniciando feature: {featureContext.FeatureInfo.Title}");
    }

    [AfterFeature]
    public static void AfterFeature(FeatureContext featureContext)
    {
        Console.WriteLine($"Feature concluída: {featureContext.FeatureInfo.Title}");
    }
}