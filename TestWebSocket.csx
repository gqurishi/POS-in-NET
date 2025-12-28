#!/usr/bin/env dotnet script

// Simple WebSocket Test Script
// Run with: dotnet script TestWebSocket.csx

using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

async Task TestWebSocket(string url)
{
    Console.WriteLine($"🔍 Testing: {url.Substring(0, Math.Min(60, url.Length))}...");
    
    try
    {
        using var ws = new ClientWebSocket();
        ws.Options.SetRequestHeader("X-Tenant-ID", "kitchen");
        ws.Options.SetRequestHeader("X-API-Key", "pos_51df235474f5ba2371c0dc335feac228978e84e64a9ededa3f8e50e85edb3122");
        
        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        
        await ws.ConnectAsync(new Uri(url), cts.Token);
        
        Console.WriteLine("✅ CONNECTED! WebSocket is working!");
        Console.WriteLine($"   State: {ws.State}");
        Console.WriteLine($"   Use this URL in your POS app!");
        Console.WriteLine();
        
        // Try to receive a message
        var buffer = new byte[4096];
        var receiveTask = ws.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
        var timeoutTask = Task.Delay(3000);
        
        var completedTask = await Task.WhenAny(receiveTask, timeoutTask);
        
        if (completedTask == receiveTask)
        {
            var result = await receiveTask;
            var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
            Console.WriteLine($"📨 Received: {message.Substring(0, Math.Min(100, message.Length))}");
        }
        else
        {
            Console.WriteLine("⏱️  No messages yet (this is OK for idle connection)");
        }
        
        await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Test complete", CancellationToken.None);
        return true;
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ Failed: {ex.Message}");
        return false;
    }
}

Console.WriteLine("🔬 WebSocket Connection Test for OrderWeb.net");
Console.WriteLine("============================================");
Console.WriteLine();

var urls = new[]
{
    "wss://orderweb.net/ws/pos/kitchen?apiKey=pos_51df235474f5ba2371c0dc335feac228978e84e64a9ededa3f8e50e85edb3122",
    "wss://orderweb.net:9011?apiKey=pos_51df235474f5ba2371c0dc335feac228978e84e64a9ededa3f8e50e85edb3122",
    "wss://orderweb.net/ws/kitchen?apiKey=pos_51df235474f5ba2371c0dc335feac228978e84e64a9ededa3f8e50e85edb3122"
};

foreach (var url in urls)
{
    var success = await TestWebSocket(url);
    if (success)
    {
        Console.WriteLine();
        Console.WriteLine("✅ SUCCESS! Update your POS app with this WebSocket URL.");
        Environment.Exit(0);
    }
    Console.WriteLine();
    await Task.Delay(1000); // Wait between tests
}

Console.WriteLine("⚠️  All WebSocket URLs failed.");
Console.WriteLine();
Console.WriteLine("📞 Contact OrderWeb.net support and ask:");
Console.WriteLine("   1. What is the correct WebSocket URL for tenant 'kitchen'?");
Console.WriteLine("   2. Is WebSocket enabled for my account?");
Console.WriteLine("   3. Do I need special authentication?");
