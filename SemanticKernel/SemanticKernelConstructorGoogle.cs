using Microsoft.SemanticKernel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GreyMail.SemanticKernel
{
    internal class SemanticKernelConstructorGoogle: ISemanticKernelConstructorOllama
    {
        public IKernelBuilder KernelBuilder { get; }
        public Kernel Kernel { get; }
        public SemanticKernelConstructorGoogle()
        {
            Config.Instance.Load();
            KernelBuilder = Kernel.CreateBuilder();
            Kernel = Kernel.CreateBuilder()
                .AddGoogleAIGeminiChatCompletion("gemini-pro", "")
                .Build();

        }
    }
}
