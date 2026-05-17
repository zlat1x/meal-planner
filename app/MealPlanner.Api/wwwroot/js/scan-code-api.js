const foodsApi = "/api/Foods";
const scanCodesApi = "/api/food-scan-codes";

let cameraStream = null;
let scanTimer = null;

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
        codeValue: document.getElementById("codeValueInput").value,
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
            codeValue: codeValue,
            source: source
        })
    });

    const data = await response.json();

    if (!response.ok || !data.found) {
        showScanError(data.message ?? "Продукт не знайдено.");
        await loadScanLogs();
        return;
    }

    const container = document.getElementById("scanResult");
    container.innerHTML = `
        <div class="summary">
            <b>${data.foodName}</b><br>
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
    if (!("BarcodeDetector" in window)) {
        showScanError("BarcodeDetector не підтримується цим браузером. Використайте Chrome або ручне введення коду.");
        return;
    }

    const detector = new BarcodeDetector({
        formats: ["qr_code", "ean_13", "ean_8", "code_128", "code_39", "upc_a", "upc_e"]
    });

    const video = document.getElementById("cameraPreview");

    cameraStream = await navigator.mediaDevices.getUserMedia({
        video: {
            facingMode: "environment"
        }
    });

    video.srcObject = cameraStream;
    video.style.display = "block";

    scanTimer = setInterval(async () => {
        const codes = await detector.detect(video);

        if (codes.length > 0) {
            const value = codes[0].rawValue;
            document.getElementById("scanInput").value = value;
            await scanCode(value, "Camera");
            stopCameraScanner();
        }
    }, 1000);
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

function showScanError(message) {
    const container = document.getElementById("scanResult");
    container.innerHTML = `<p class="error">${message}</p>`;
}

loadFoodsForSelect();
loadScanLogs();
