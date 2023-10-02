const twitchChatListeners = [];
const framesListeners = [];

const client = new tmi.Client({
    channels: ["darkviperau"]
});
var reconnectTimeout = null;

client.on('message', async (channel, tags, message, self) => {
    for (const listener of twitchChatListeners) {
        await listener.invokeMethodAsync("OnTwitchChatMessage", {
            channel,
            message,
            messageId: tags.id,
            userId: tags['user-id'],
            userLogin: tags.username,
            userDisplayName: tags['display-name']
        });
    }
});

client.on('roomstate', (channel, state) => {
    console.log('roomstate', channel);
    if (reconnectTimeout) {
        clearTimeout(reconnectTimeout);
        reconnectTimeout = null;
    }
});

(async () => {
    await client.connect();

    const reconnect = async () => {
        await client.disconnect();
        await client.connect();
        reconnectTimeout = setTimeout(reconnect, 10000);
    }
    reconnectTimeout = setTimeout(reconnect, 10000);
})();

const framesCallback = async () => {
    for (const listener of framesListeners)
        await listener.invokeMethodAsync("OnFrame");
    requestAnimationFrame(framesCallback);
};
requestAnimationFrame(framesCallback);

function twitchChatAddListener(dotnetRef) {
    twitchChatListeners.push(dotnetRef);
}

function twitchChatRemoveListener(dotnetRef) {
    const index = twitchChatListeners.indexOf(dotnetRef);
    if (0 <= index)
        twitchChatListeners.splice(index, 1);
}

function framesAddListener(dotnetRef) {
    framesListeners.push(dotnetRef);
}

function framesRemoveListener(dotnetRef) {
    const index = framesListeners.indexOf(dotnetRef);
    if (0 <= index)
        framesListeners.splice(index, 1);
}
