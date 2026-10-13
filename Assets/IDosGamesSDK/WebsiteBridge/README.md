# Unity ↔ website bridge

1. Add `IDosWebsiteBridge` to a GameObject with a unique name, e.g. `IDosBridge`.
2. Keep the `.jslib` file inside its `Plugins/WebGL` folder. Unity includes it in
   the next WebGL build. No changes to scenes are made by these files.
3. The website must expose `window.YesURBridge` in the same frame as Unity.
   The published website version 9 already does this after sign-in.

## Unity → website

```csharp
bridge.SendJson("{\"action\":\"hello\",\"score\":100}");
bridge.RequestInventory();
bridge.ReportGameCompleted();
```

`bridge` is a reference to the IDosWebsiteBridge component. Call the last method
only at the actual game-completion point. It sends a report, not a reward grant.

On the website, receive arbitrary JSON:

```js
window.addEventListener("unity:data", event => console.log(event.detail));
```

Inventory and achievement calls receive replies automatically through the public
`ReceiveDataFromSite(string json)` method. Subscribe to `bridge.DataReceived`, or
connect the Inspector's On Data Received event to your own string receiver.

Example inventory reply:

```json
{"type":"inventory","ok":true,"data":{"items":[{"itemId":"golden_skeleton","quantity":1,"instances":[{"instanceId":"server-instance-id","level":0}]}]}}
```

An empty items array means no owned items. On error, `ok` is false and `error`
contains the message. Parse with your own serializable DTOs / JsonUtility.

## Website → Unity

```js
unityInstance.SendMessage("IDosBridge", "ReceiveDataFromSite", JSON.stringify({ action: "hello" }));
await window.YesURBridge.sendItemToGame("golden_skeleton", "IDosBridge", "ReceiveDataFromSite");
```

The item method checks server ownership. Its payload is the item object directly
(itemId, quantity, instances), without the inventory envelope. Your game's receiver
must apply the skin/model; this bridge only passes data. Sending an item neither
consumes it nor creates an NFT, and SendMessage does not acknowledge application.

`ReportGameCompleted` currently returns `pending_verification`. The trusted
server-verification step for the 2-YUA reward is not connected. An `ok: true`
reply means the report was accepted, not that tokens were awarded.

The Unity Editor has no website connection: RequestInventory/ReportGameCompleted
return an explicit error. Test the real flow in a newly compiled WebGL build.
No authentication ticket or wallet key is transmitted into Unity.
