const foodsApi = "/api/Foods";
const scanCodesApi = "/api/food-scan-codes";

let cameraStream = null;
let scanTimer = null;
let zxingReader = null;

async function loadFoodsForSelect() {
    const response = await fetch(foodsApi);
    const foods = await response.json();

    const select = document.getElementById("foodSelect");
    select.innerHTML = "";

    foods.forEach(food => {
        const option = document.createElement("option");
        option.value = food.id;
        option.textContent = `${food.name} (${food.category})`;
        option.dataset.userId = food.userId;
        select.appendChild(option);
    });

    if (foods.length > 0) {
        document.getElementById("userIdInput").value = foods[0].userId;
    }

    select.addEventListener("change", () => {
        const selectedOption = select.options[select.selectedIndex];
        document.getElementById("userIdInput").value = selectedOption.dataset.userId;
    });
}

async function createScanCode() {
    const request = {
        foodId: document.getElementById("foodSelect").value,
        userId: document.getElementById("userIdInput").value,
        codeValue: document.getElementById("codeValueInput").value.trim(),
        codeType: document.getElementById("codeTypeInput").value,
        note: "Created from JavaScript client"
    };

    const response = await fetch(scanCodesApi, {
        method: "POST",
        headers: {
            "Accept": "application/json",
            "Content-Type": "application/json"
        },
        body: JSON.stringify(request)
    });

    const resultText = document.getElementById("bindResult");

    if (!response.ok) {
        resultText.textContent = await response.text();
        resultText.className = "error";
        return;
    }

    const result = await response.json();
    resultText.textContent = `Код ${result.codeValue} прив'язано до продукту ${result.foodName}.`;
    resultText.className = "";
}

async function scanManualCode() {
    const codeValue = document.getElementById("scanInput").value.trim();

    if (codeValue.length === 0) {
        showScanError("Введіть або відскануйте код.");
        return;
    }

    await scanCode(codeValue, "Manual input");
}

async function scanCode(codeValue, source) {
    const response = await fetch(`${scanCodesApi}/scan`, {
        method: "POST",
        headers: {
            "Accept": "application/json",
            "Content-Type": "application/json"
        },
        body: JSON.stringify({
            codeValue: codeValue.trim(),
            source: source
        })
    });

    const data = await response.json();

    if (!response.ok || !data.found) {
        showScanError(`${data.message ?? "Продукт не знайдено."} Розпізнаний код: ${codeValue}`);
        await loadScanLogs();
        return;
    }

    const container = document.getElementById("scanResult");
    container.innerHTML = `
        <div class="summary">
            <b>${data.foodName}</b><br>
            Розпізнаний код: ${codeValue}<br>
            Категорія: ${data.category}<br>
            Білки: ${data.proteinPer100} г,
            Вуглеводи: ${data.carbsPer100} г,
            Жири: ${data.fatPer100} г,
            Калорійність: ${data.kcalPer100} ккал
        </div>
    `;

    await loadScanLogs();
}

async function startCameraScanner() {
    const video = document.getElementById("cameraPreview");

    if ("BarcodeDetector" in window) {
        await startNativeCameraScanner(video);
        return;
    }

    if (window.ZXing) {
        await startZxingCameraScanner(video);
        return;
    }

    showScanError("Браузер не підтримує вбудований BarcodeDetector, а fallback-бібліотека ZXing не завантажилась. Спробуйте Chrome або кнопку «Завантажити фото з файлу».");
}

async function startNativeCameraScanner(video) {
    const detector = new BarcodeDetector({
        formats: ["qr_code", "ean_13", "ean_8", "code_128", "code_39", "upc_a", "upc_e"]
    });

    try {
        cameraStream = await navigator.mediaDevices.getUserMedia({
            video: {
                facingMode: "environment"
            }
        });

        video.srcObject = cameraStream;
        video.style.display = "block";

        scanTimer = setInterval(async () => {
            if (video.readyState < 2) {
                return;
            }

            const codes = await detector.detect(video);

            if (codes.length > 0) {
                const value = codes[0].rawValue;
                document.getElementById("scanInput").value = value;
                await scanCode(value, "Camera BarcodeDetector");
                stopCameraScanner();
            }
        }, 1000);
    } catch {
        showScanError("Не вдалося увімкнути камеру. Дозвольте доступ до камери або відкрийте сторінку через HTTPS/localhost.");
    }
}

async function startZxingCameraScanner(video) {
    try {
        zxingReader = new ZXing.BrowserMultiFormatReader();
        video.style.display = "block";

        const devices = await zxingReader.listVideoInputDevices();
        const backCamera = devices.find(device =>
            device.label.toLowerCase().includes("back") ||
            device.label.toLowerCase().includes("rear") ||
            device.label.toLowerCase().includes("environment")
        );

        const deviceId = backCamera?.deviceId ?? devices[0]?.deviceId;

        if (!deviceId) {
            showScanError("Камеру не знайдено. Перевірте дозвіл браузера на використання камери.");
            return;
        }

        await zxingReader.decodeFromVideoDevice(deviceId, video, async (result) => {
            if (!result) {
                return;
            }

            const value = result.getText();
            document.getElementById("scanInput").value = value;
            await scanCode(value, "Camera ZXing");
            stopCameraScanner();
        });
    } catch {
        showScanError("Не вдалося запустити ZXing-сканер. Спробуйте кнопку «Завантажити фото з файлу» або ручне введення.");
    }
}

function openPhotoScanner() {
    document.getElementById("photoInput").click();
}

function openFileScanner() {
    document.getElementById("fileInput").click();
}

async function scanPhotoCode(event) {
    const file = event.target.files[0];

    if (!file) {
        return;
    }

    const imageUrl = URL.createObjectURL(file);

    try {
        showScanInfo("Обробляю зображення. Якщо код маленький або розмитий, оберіть чіткіше фото.");

        const value = await readCodeFromPhoto(imageUrl);

        document.getElementById("scanInput").value = value;
        await scanCode(value, "Image upload scan");
    } catch {
        showScanError("Не вдалося розпізнати код із зображення. Для демо завантажте саме PNG/JPG з QR-кодом, не скрін у Telegram і не зменшену превʼюшку.");
    } finally {
        URL.revokeObjectURL(imageUrl);
        event.target.value = "";
    }
}

async function readCodeFromPhoto(imageUrl) {
    const jsQrValue = await tryReadPhotoWithJsQr(imageUrl);

    if (jsQrValue) {
        return jsQrValue;
    }

    const nativeValue = await tryReadPhotoWithBarcodeDetector(imageUrl);

    if (nativeValue) {
        return nativeValue;
    }

    const zxingValue = await tryReadPhotoWithZxing(imageUrl);

    if (zxingValue) {
        return zxingValue;
    }

    const enlargedImageUrl = await createEnlargedImageUrl(imageUrl);

    try {
        const enlargedJsQrValue = await tryReadPhotoWithJsQr(enlargedImageUrl);

        if (enlargedJsQrValue) {
            return enlargedJsQrValue;
        }

        const enlargedZxingValue = await tryReadPhotoWithZxing(enlargedImageUrl);

        if (enlargedZxingValue) {
            return enlargedZxingValue;
        }
    } finally {
        URL.revokeObjectURL(enlargedImageUrl);
    }

    throw new Error("Code was not recognized.");
}

async function tryReadPhotoWithJsQr(imageUrl) {
    if (!window.jsQR) {
        return null;
    }

    try {
        const image = await loadImage(imageUrl);
        const canvas = document.createElement("canvas");
        const context = canvas.getContext("2d", { willReadFrequently: true });

        canvas.width = image.naturalWidth;
        canvas.height = image.naturalHeight;
        context.drawImage(image, 0, 0);

        const imageData = context.getImageData(0, 0, canvas.width, canvas.height);
        const code = jsQR(imageData.data, imageData.width, imageData.height, {
            inversionAttempts: "attemptBoth"
        });

        return code?.data ?? null;
    } catch {
        return null;
    }
}

async function tryReadPhotoWithBarcodeDetector(imageUrl) {
    if (!("BarcodeDetector" in window)) {
        return null;
    }

    try {
        const detector = new BarcodeDetector({
            formats: ["qr_code", "ean_13", "ean_8", "code_128", "code_39", "upc_a", "upc_e"]
        });

        const image = await loadImage(imageUrl);
        const codes = await detector.detect(image);

        if (codes.length === 0) {
            return null;
        }

        return codes[0].rawValue;
    } catch {
        return null;
    }
}

async function tryReadPhotoWithZxing(imageUrl) {
    if (!window.ZXing) {
        return null;
    }

    try {
        const reader = new ZXing.BrowserMultiFormatReader();
        const result = await reader.decodeFromImageUrl(imageUrl);

        return result.getText();
    } catch {
        return null;
    }
}

function loadImage(imageUrl) {
    return new Promise((resolve, reject) => {
        const image = new Image();

        image.onload = () => resolve(image);
        image.onerror = reject;
        image.src = imageUrl;
    });
}

async function createEnlargedImageUrl(imageUrl) {
    const image = await loadImage(imageUrl);
    const canvas = document.createElement("canvas");
    const scale = 3;

    canvas.width = image.naturalWidth * scale;
    canvas.height = image.naturalHeight * scale;

    const context = canvas.getContext("2d");
    context.imageSmoothingEnabled = false;
    context.drawImage(image, 0, 0, canvas.width, canvas.height);

    return new Promise(resolve => {
        canvas.toBlob(blob => resolve(URL.createObjectURL(blob)), "image/png");
    });
}

function stopCameraScanner() {
    if (scanTimer !== null) {
        clearInterval(scanTimer);
        scanTimer = null;
    }

    if (cameraStream !== null) {
        cameraStream.getTracks().forEach(track => track.stop());
        cameraStream = null;
    }

    if (zxingReader !== null) {
        zxingReader.reset();
        zxingReader = null;
    }

    document.getElementById("cameraPreview").style.display = "none";
}

async function loadScanLogs() {
    const response = await fetch(`${scanCodesApi}/logs`);
    const logs = await response.json();

    const body = document.getElementById("logsBody");
    body.innerHTML = "";

    logs.forEach(log => {
        const row = body.insertRow();

        row.insertCell(0).innerText = log.scannedCode;
        row.insertCell(1).innerText = log.result;
        row.insertCell(2).innerText = log.foodName ?? "-";
        row.insertCell(3).innerText = log.source;
        row.insertCell(4).innerText = new Date(log.scannedAt).toLocaleString("uk-UA");
    });
}

function showScanInfo(message) {
    const container = document.getElementById("scanResult");
    container.innerHTML = `<p class="hint">${message}</p>`;
}

function showScanError(message) {
    const container = document.getElementById("scanResult");
    container.innerHTML = `<p class="error">${message}</p>`;
}

loadFoodsForSelect();
loadScanLogs();