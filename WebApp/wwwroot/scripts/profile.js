let clockTimer;

function getBrowser(userAgent) {
    const browsers = [
        [/Edg\/([\d.]+)/, "Microsoft Edge"],
        [/OPR\/([\d.]+)/, "Opera"],
        [/Firefox\/([\d.]+)/, "Firefox"],
        [/Chrome\/([\d.]+)/, "Google Chrome"],
        [/Version\/([\d.]+).*Safari/, "Safari"]
    ];

    for (const [pattern, name] of browsers) {
        const match = userAgent.match(pattern);
        if (match) return `${name} ${match[1]}`;
    }

    return "نامشخص";
}

function getOperatingSystem(userAgent) {
    if (/Windows/i.test(userAgent)) return "Windows";
    if (/Android/i.test(userAgent)) return "Android";
    if (/iPhone|iPad|iPod/i.test(userAgent)) return "iOS / iPadOS";
    if (/Mac OS X/i.test(userAgent)) return "macOS";
    if (/Linux/i.test(userAgent)) return "Linux";
    return "نامشخص";
}

export async function getClientDeviceInfo() {
    const response = await fetch("/account/device-info", {
        credentials: "same-origin",
        headers: { "Accept": "application/json" }
    });
    const serverInfo = response.ok ? await response.json() : { ip: "نامشخص" };

    return {
        ip: serverInfo.ip,
        browser: getBrowser(navigator.userAgent),
        operatingSystem: getOperatingSystem(navigator.userAgent)
    };
}

export function startLocalClock(elementId) {
    stopLocalClock();

    const formatter = new Intl.DateTimeFormat("fa-IR-u-ca-persian", {
        dateStyle: "medium",
        timeStyle: "medium"
    });

    const update = () => {
        const element = document.getElementById(elementId);
        if (element) element.textContent = formatter.format(new Date());
    };

    update();
    clockTimer = window.setInterval(update, 1000);
}

export function stopLocalClock() {
    if (clockTimer) {
        window.clearInterval(clockTimer);
        clockTimer = undefined;
    }
}
