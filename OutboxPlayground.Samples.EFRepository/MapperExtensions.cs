using OutboxPlayground.Samples.Abstractions;

// docs: https://mapperly.riok.app/docs/getting-started/installation/
using Riok.Mapperly.Abstractions;

namespace OutboxPlayground.Samples.EFRepository;

[Mapper]
internal static partial class MapperExtensions
{
    [MapperIgnoreSource(nameof(PaymentRequest.UserName))]
    internal static partial PaymentMessage ToMessage(this PaymentRequest payment, Risk riskAssessment);

    [MapperIgnoreSource(nameof(PaymentRequest.UserId))]
    [MapperIgnoreSource(nameof(PaymentRequest.UserName))]
    internal static partial PaymentEntity ToEntity(this PaymentRequest payment);
}
