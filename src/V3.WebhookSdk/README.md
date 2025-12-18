# V3.WebhookSdk

Biblioteca .NET para processar webhooks da V3 Tecnologia baseada em Protobuf.

## Features
- Parsing de eventos JSON em tipos fortemente tipados (Protobuf)
- Builder pattern para configuração de handlers por categoria/evento
- Roteamento inteligente via dicionário (sem if/switch)
- Helpers como `.WithDmsHandler`, `.WithOrderHandler`, etc.
- Não depende de servidor HTTP

## Instalação

```
dotnet add package V3.WebhookSdk
```

## Exemplo de Uso

```csharp
var processor = new WebhookEventProcessorBuilder()
    .WithDmsHandler("DROWSINESS", async (ctx, evt) => { /* ... */ })
    .WithOrderHandler("ORDER_STATUS_ACK", async (ctx, evt) => { /* ... */ })
    .Build();

await processor.ProcessWebhookAsync(jsonPayload);
```

## Estrutura
- Protos/: arquivos .proto
- Generated/: código C# gerado
- Processing/: núcleo do SDK
- Handlers/: interfaces/delegates
- Builders/: builder pattern
- Models/: tipos auxiliares
- Utils/: utilitários
- Examples/: exemplos de uso

## Geração dos Protos
Utilize Grpc.Tools para gerar os arquivos C# a partir dos .proto.

## Licença
MIT
