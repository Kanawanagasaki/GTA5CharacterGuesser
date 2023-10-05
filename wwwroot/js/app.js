const twitchChatListeners = [];
const framesListeners = [];
const audioMap = {};

const videoDescription = {
    filename: null,
    controller: null,
    signal: null,
    isDownloaded: false,
    bytes: null
};

const client = new tmi.Client({
    channels: ["darkviperau"]
});
var reconnectTimeout = null;
var AesGcmKey = null;
var AesGcmIv = null;

client.on('message', async (channel, tags, message, self) => {
    for (const listener of twitchChatListeners) {
        await listener.invokeMethodAsync("OnTwitchChatMessage", {
            channel,
            message,
            messageId: tags.id,
            userId: tags['user-id'],
            userLogin: tags.username,
            userDisplayName: tags['display-name'],
            color: tags.color
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

async function prepareAudio(dotnetRef, filename) {
    if (!audioMap[filename]) {
        audioMap[filename] = new Audio();
        audioMap[filename].addEventListener("ended", async () => {
            await dotnetRef.invokeMethodAsync("OnAudioEnded", filename);
        });

        const arrayBuffer = await fetchAndDecrypt("encrypted/" + filename);
        if (arrayBuffer) {
            const blob = new Blob([arrayBuffer]);
            audioMap[filename].src = URL.createObjectURL(blob);
        }
    }

    audioMap[filename].pause();
    audioMap[filename].currentTime = 0;
}

async function playAudio(dotnetRef, filename) {
    if (!audioMap[filename]) {
        audioMap[filename] = new Audio();
        audioMap[filename].addEventListener("ended", async () => {
            await dotnetRef.invokeMethodAsync("OnAudioEnded", filename);
        });

        const arrayBuffer = await fetchAndDecrypt("encrypted/" + filename);
        if (arrayBuffer) {
            const blob = new Blob([arrayBuffer]);
            audioMap[filename].src = URL.createObjectURL(blob);
        }
    }

    audioMap[filename].currentTime = 0;
    audioMap[filename].play();
}

async function stopAudio(dotnetRef, filename) {
    if (!audioMap[filename])
        return;

    audioMap[filename].pause();
    audioMap[filename].currentTime = 0;
    await dotnetRef.invokeMethodAsync("OnAudioEnded", filename);
}

async function prepareVideo(dotnetRef, filename) {
    try {
        if (videoDescription.filename == filename)
            return;

        if (videoDescription.controller)
            videoDescription.controller.abort();

        videoDescription.isDownloaded = false;

        videoDescription.filename = filename;
        videoDescription.controller = new AbortController();
        videoDescription.signal = videoDescription.controller.signal;

        videoDescription.bytes = await fetchAndDecrypt("encrypted/" + filename, videoDescription.signal);
        videoDescription.isDownloaded = true;
        await dotnetRef.invokeMethodAsync("OnVideoPrepared", filename);
    }
    catch
    {
        videoDescription.isDownloaded = false;
    }
}

async function loadVideo(dotnetRef, videoEl, filename) {
    if (videoDescription.filename != filename)
        await prepareVideo(dotnetRef, filename);

    const onReady = () => {
        const blob = new Blob([videoDescription.bytes]);
        videoEl.src = URL.createObjectURL(blob);
        videoEl.play();
    };

    if(videoDescription.filename == filename && videoDescription.isDownloaded)
        onReady();
    else
    {
        const intervalId = setInterval(() => {
            if (!videoDescription.isDownloaded)
                return;
    
            clearInterval(intervalId);

            if(filename == videoDescription.filename)
                onReady();
        }, 200);
    }
}

async function setupAesGcm(input) {
    try {
        if (typeof input !== "string")
            return;
        if (input.length == 0)
            return;
        while (input.length < 48)
            input += input;
        if (input.length > 48)
            input = input.substring(0, 48);
        const textEncoder = new TextEncoder();
        var inputBuffer = textEncoder.encode(input.substring(0, 32));
        AesGcmKey = await crypto.subtle.importKey("raw", inputBuffer, "AES-GCM", false, ["decrypt"]);
        AesGcmIv = textEncoder.encode(input.substring(32, 48));
    }
    catch
    {
        AesGcmKey = null;
        AesGcmIv = null;
    }
}

async function getData() {
    try {
        if (AesGcmKey === null || AesGcmIv === null)
            return null;

        const decrypted = await fetchAndDecrypt("encrypted/data.bin");
        if (decrypted === null)
            return null;
        return JSON.parse(new TextDecoder().decode(decrypted));
    }
    catch {
        return null;
    }
}

async function fetchAndDecrypt(url, signal = null) {
    const response = await fetch(url, { method: "get", signal });
    if (!response.ok)
        return null;
    const encrypted = await response.arrayBuffer();
    return await crypto.subtle.decrypt({ name: "AES-GCM", iv: AesGcmIv }, AesGcmKey, encrypted);
}

