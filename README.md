# VeloxVox

[![NuGet version](https://img.shields.io/nuget/v/VeloxVox.svg?style=for-the-badge)](https://www.nuget.org/packages/VeloxVox/)
[![.NET](https://img.shields.io/badge/.NET-8.0-blueviolet?style=for-the-badge)](https://dotnet.microsoft.com/download/dotnet/8.0)
[![Platform](https://img.shields.io/badge/Platform-Windows-blue.svg?style=for-the-badge)](https://docs.microsoft.com/en-us/windows/apps/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg?style=for-the-badge)](https://github.com/MrRoxandi/VeloxVox?tab=MIT-1-ov-file)

**VeloxVox** is a high-performance, modern audio playback and Text-To-Speech (TTS) engine for .NET on Windows. Built on the robust foundations of **LibVLCSharp** and **System.Speech**, it's designed for speed, responsiveness, and a simple, intuitive API. Whether you're adding voice notifications to a desktop app or building a complex audio queuing system, VeloxVox provides a solid and extensible solution.

## Features

- 🚀 **High Performance**: Asynchronous-first design with a background processing queue ensures your application's UI remains responsive, even during audio synthesis or playback.
- 🗣️ **High-Quality TTS**: Integrated Text-To-Speech powered by the native `System.Speech` synthesizer, with easy options to control voice, rate, and volume.
- 🎶 **Versatile Playback**: Leverages the power of LibVLC to play a vast range of audio formats from local files, network streams, and URLs. All in a headless mode with no visible UI.
- 🔌 **Pluggable Architecture**: Easily replace the default audio player or TTS engine with your own implementation by conforming to simple interfaces (`IAudioPlayer`, `ITtsEngine`).
- ✨ **Fluent Configuration**: A clean, fluent builder API (`VeloxVoxBuilder`) for setting up your audio engine exactly how you need it.
- 🔔 **Rich Events**: Subscribe to events for key playback states like `QueueItemStarted`, `QueueItemCompleted`, and `QueueItemFailed` to build interactive experiences.

## Installation

Install the package from NuGet Package Manager or via the .NET CLI:

```sh
dotnet add package VeloxVox
```

> **Note:** VeloxVox targets Windows and relies on `System.Speech` and native VLC libraries.

## Quick Start

The easiest way to start is by using the `VeloxVoxBuilder`. This example creates an engine and says "Hello, World!".

```csharp
using VeloxVox;

// In your async Main method or an event handler
public static async Task Main(string[] args)
{
    Console.WriteLine("Initializing VeloxVox...");

    // 1. Build the engine. `await using` ensures it's properly disposed.
    await using var engine = await new VeloxVoxBuilder().BuildAsync();

    // 2. Enqueue text to be spoken.
    await engine.EnqueueTtsAsync("Hello, VeloxVox! Playback is starting now.");

    Console.WriteLine("TTS enqueued. Press any key to exit.");
    Console.ReadKey(); // Keep the app alive to hear the sound.
}
```

## Enqueuing Different Audio Sources

VeloxVox manages a queue, allowing you to add audio from multiple sources sequentially.

```csharp
await using var engine = await new VeloxVoxBuilder().BuildAsync();

// 1. Enqueue Text-To-Speech with custom options
await engine.EnqueueTtsAsync(
    "First, we play this message with a faster voice.",
    new() { Rate = 3 } // Rate can be from -10 to 10
);

// 2. Enqueue an audio file from the internet
engine.EnqueueUrl("https://www.soundhelix.com/examples/mp3/SoundHelix-Song-1.mp3");

// 3. Enqueue a local file
string localFilePath = @"C:\path\to\your\audio.wav";
if (File.Exists(localFilePath))
{
    engine.EnqueueFile(localFilePath);
}

Console.WriteLine("Audio queue is populated. Playback will proceed in order.");
Console.ReadKey();
```

## Using with Dependency Injection

VeloxVox is fully compatible with DI containers. This is the recommended approach for larger applications.

### 1. Register VeloxVox Services

In your `Program.cs` or `Startup.cs`:

```csharp
using Microsoft.Extensions.DependencyInjection;
using VeloxVox;
using VeloxVox.Abstractions; // For ILogger if you use it

// ...
var services = new ServiceCollection();

// 1. Build the engine once and register it as a singleton.
// This is important as it manages a single audio output.
var veloxVoxEngine = await new VeloxVoxBuilder().BuildAsync();
services.AddSingleton(veloxVoxEngine);

// 2. Register your services that will use the engine
services.AddTransient<MyAudioNotificationService>();

var serviceProvider = services.BuildServiceProvider();

// Remember to dispose the engine when the application shuts down
// If using a host builder, this can be handled in application stopping events.
// await serviceProvider.GetRequiredService<VeloxVoxEngine>().DisposeAsync();
```

### 2. Inject `VeloxVoxEngine` into Your Services

```csharp
public class MyAudioNotificationService
{
    private readonly VeloxVoxEngine _audioEngine;

    public MyAudioNotificationService(VeloxVoxEngine audioEngine)
    {
        _audioEngine = audioEngine;
    }

    public async Task NotifyUser(string message)
    {
        _audioEngine.ClearQueue(); // Clear any previous notifications
        await _audioEngine.EnqueueTtsAsync($"Attention! {message}");
    }
}
```

## Configuration In-Depth

The `VeloxVoxBuilder` gives you full control over the engine's components.

```csharp
// Example of swapping out backends
var myCustomPlayer = new MyCustomAudioPlayer();
var myCustomTts = new MyCloudBasedTtsEngine();

await using var engine = await new VeloxVoxBuilder()
    .WithAudioPlayer(myCustomPlayer) // Provide your own IAudioPlayer backend
    .WithTtsEngine(myCustomTts)      // Provide your own ITtsEngine backend
    .BuildAsync();
```

## Handling Events

You can build a rich, interactive UI by subscribing to the engine's events.

```csharp
engine.QueueItemStarted += (sender, args) =>
{
    Console.WriteLine($"[EVENT] Now Playing: {args.Item.SourcePath}");
};

engine.QueueItemCompleted += (sender, args) =>
{
    Console.WriteLine($"[EVENT] Finished: {args.Item.SourcePath} (Reason: {args.Reason})");
};

engine.QueueEmpty += (sender, args) =>
{
    Console.WriteLine($"[EVENT] The playback queue is now empty.");
};
```

## Contributing

Contributions are welcome! If you find a bug, have a feature request, or want to improve the code, please feel free to open an issue or submit a pull request.

## License

This project is licensed under the MIT License. See the [LICENSE](LICENSE.txt) file for details.
