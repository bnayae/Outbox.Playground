using OutboxPlayground.Samples.Abstractions;
using Riok.Mapperly.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ConstrainedExecution;
using System.Text;
using System.Threading.Tasks;

namespace OutboxPlayground.Samples.EFRepository;

[Mapper]
public static partial class MapperExtensions
{
    public static partial PaymentMessage ToMessage(this Payment payment, Risk riskAssessment);
}
