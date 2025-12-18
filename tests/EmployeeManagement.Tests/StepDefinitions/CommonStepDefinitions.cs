using TechTalk.SpecFlow;
using EmployeeManagement.Domain.Enums;

namespace EmployeeManagement.Tests.StepDefinitions;

[Binding]
public class CommonStepDefinitions
{
    private readonly ScenarioContext _scenarioContext;

    public CommonStepDefinitions(ScenarioContext scenarioContext)
    {
        _scenarioContext = scenarioContext;
    }

    [Given(@"que o usuário está autenticado como ""(.*)""")]
    public void DadoQueOUsuarioEstaAutenticadoComo(string role)
    {
        var parsedRole = Enum.Parse<Role>(role);
        _scenarioContext.Set(parsedRole, "CurrentUserRole");
        _scenarioContext.Set(role, "UserRole");
        _scenarioContext.Set(true, "IsAuthenticated");
    }

    [Given(@"que o usuário não está autenticado")]
    public void DadoQueOUsuarioNaoEstaAutenticado()
    {
        _scenarioContext.Set(false, "IsAuthenticated");
    }
}