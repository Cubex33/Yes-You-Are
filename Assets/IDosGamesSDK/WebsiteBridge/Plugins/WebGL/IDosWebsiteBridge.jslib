mergeInto(LibraryManager.library, {
  SendDataToSite: function (jsonPointer) {
    try {
      var data = JSON.parse(UTF8ToString(jsonPointer));
      window.dispatchEvent(new CustomEvent("unity:data", { detail: data }));
    } catch (error) {
      console.error("[Website bridge] Invalid JSON:", error);
    }
  },

  YesUR_RequestInventory: function (receiverPointer) {
    var receiver = UTF8ToString(receiverPointer);
    var send = Module.SendMessage.bind(Module);
    Promise.resolve().then(function () {
      if (!window.YesURBridge || typeof window.YesURBridge.getInventory !== "function")
        throw new Error("Website inventory API is not available. Sign in and launch the game on the website.");
      return window.YesURBridge.getInventory();
    }).then(function (inventory) {
      send(receiver, "ReceiveDataFromSite", JSON.stringify({ type: "inventory", ok: true, data: inventory }));
    }, function (error) {
      send(receiver, "ReceiveDataFromSite", JSON.stringify({ type: "inventory", ok: false, error: error.message || String(error) }));
    });
  },

  YesUR_ReportAchievement: function (receiverPointer) {
    var receiver = UTF8ToString(receiverPointer);
    var send = Module.SendMessage.bind(Module);
    Promise.resolve().then(function () {
      if (!window.YesURBridge || typeof window.YesURBridge.reportAchievement !== "function")
        throw new Error("Website achievement API is not available. Sign in and launch the game on the website.");
      return window.YesURBridge.reportAchievement("game_completed");
    }).then(function (result) {
      send(receiver, "ReceiveDataFromSite", JSON.stringify({ type: "achievement", ok: true, data: result }));
    }, function (error) {
      send(receiver, "ReceiveDataFromSite", JSON.stringify({ type: "achievement", ok: false, error: error.message || String(error) }));
    });
  }
});
