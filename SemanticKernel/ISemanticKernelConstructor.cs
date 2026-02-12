using GreyMail;
using Microsoft.SemanticKernel;
using OllamaSharp;

public interface ISemanticKernelConstructorOllama
{
   public IKernelBuilder KernelBuilder { get; }
    public Kernel Kernel { get; }
}