using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using OllamaSharp;

namespace EmailReader.SemanticKernel;

public class SemanticKernelConstructor
{
    public SemanticKernelConstructor()
    {
        Config.Instance.Load();
        KernelBuilder = Kernel.CreateBuilder();
        HttpClient httpClient = new HttpClient();
        httpClient.Timeout = TimeSpan.FromMinutes(15);
        httpClient.BaseAddress = new Uri(Config.Instance.OllamaServerUrl);
        OllamaApiClient client = new OllamaApiClient(httpClient, Config.Instance.Model);
        KernelBuilder.Services.AddKernel();
        KernelBuilder.AddOllamaChatCompletion(client);
        KernelBuilder.AddOllamaTextGeneration(client);
        KernelBuilder.AddOllamaChatClient(client);
        Kernel = KernelBuilder.Build();
        KernelBuilder.Services.AddSingleton(Kernel);
    }
    public IKernelBuilder KernelBuilder { get; }
    public Kernel Kernel { get; }
}