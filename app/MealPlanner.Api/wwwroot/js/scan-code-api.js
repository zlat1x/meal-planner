const foodsApi = "/api/Foods";
const scanCodesApi = "/api/food-scan-codes";

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
        note: "Created from API client"
    };

    if (!request.foodId || !request.userId || request.codeValue.length === 0) {
        showBindError("Оберіть продукт і введіть код.");
        return;
    }

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
    resultText.className = "success";
}

async function scanManualCode() {
    const codeValue = document.getElementById("scanInput").value.trim();

    if (codeValue.length === 0) {
        showScanError("Введіть код або завантажте зображення.");
        return;
    }

    await scanCode(codeValue, "Manual input");
}

async function scanCode(codeValue, source) {
    const normalizedCode = codeValue.trim();

    const response = await fetch(`${scanCodesApi}/scan`, {
        method: "POST",
        headers: {
            "Accept": "application/json",
            "Content-Type": "application/json"
        },
        body: JSON.stringify({
            codeValue: normalizedCode,
            source: source
        })
    });

    const data = await response.json();

    if (!response.ok || !data.found) {
        showScanError(`${data.message ?? "Продукт не знайдено."} Розпізнаний код: ${normalizedCode}`);
        await loadScanLogs();
        return;
    }

    const container = document.getElementById("scanResult");
    container.innerHTML = `
        <div class="summary">
            <b>${data.foodName}</b><br>
            Розпізнаний код: ${normalizedCode}<br>
            Категорія: ${data.category}<br>
            Білки: ${data.proteinPer100} г,<br>
            Вуглеводи: ${data.carbsPer100} г,<br>
            Жири: ${data.fatPer100} г,<br>
            Калорійність: ${data.kcalPer100} ккал
        </div>
    `;

    await loadScanLogs();
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
        showScanInfo("Обробляю зображення...");

        const value = await readCodeFromPhoto(imageUrl);

        document.getElementById("scanInput").value = value;
        await scanCode(value, "Image upload scan");
    } catch {
        showScanError("Не вдалося розпізнати код із зображення. Завантажте чіткий PNG/JPG із QR-кодом або введіть код вручну.");
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

function showBindError(message) {
    const resultText = document.getElementById("bindResult");
    resultText.textContent = message;
    resultText.className = "error";
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