const apiBaseUrl = window.mealPlannerApiBaseUrl ?? "http://localhost:5003";
const foodsApi = `${apiBaseUrl}/api/Foods`;
const scanCodesApi = `${apiBaseUrl}/api/food-scan-codes`;

let cameraStream = null;
let scanTimer = null;

async function loadFoodsForSelect() {
    try {
        const response = await fetch(foodsApi);

        if (!response.ok) {
            showBindError("Не вдалося завантажити продукти. Перевірте, чи запущений MealPlanner.Api.");
            return;
        }

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
    } catch {
        showBindError("API недоступний. Запустіть MealPlanner.Api паралельно з MealPlanner.Web.");
    }
}

async function createScanCode() {
    const request = {
        foodId: document.getElementById("foodSelect").value,
        userId: document.getElementById("userIdInput").value,
        codeValue: document.getElementById("codeValueInput").value,
        codeType: document.getElementById("codeTypeInput").value,
        note: "Created from Meal Planner Web"
    };

    if (!request.foodId || !request.userId || !request.codeValue.trim()) {
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

    if (!response.ok) {
        const message = await response.text();
        showBindError(message);
        return;
    }

    const result = await response.json();
    const resultText = document.getElementById("bindResult");
    resultText.textContent = `Код ${result.codeValue} прив'язано до продукту ${result.foodName}.`;
    resultText.className = "scan-message scan-message-success mt-3";
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
        <div class="scan-product-card">
            <h3>${data.foodName}</h3>
            <p>Категорія: ${data.category}</p>
            <div class="scan-macros">
                <span>Білки: ${data.proteinPer100} г</span>
                <span>Вуглеводи: ${data.carbsPer100} г</span>
                <span>Жири: ${data.fatPer100} г</span>
                <span>Ккал: ${data.kcalPer100}</span>
            </div>
        </div>
    `;

    await loadScanLogs();
}

async function startCameraScanner() {
    if (!("BarcodeDetector" in window)) {
        showScanError("BarcodeDetector не підтримується цим браузером. Для камери краще використати Chrome або Edge. Також можна ввести код вручну.");
        return;
    }

    if (!navigator.mediaDevices || !navigator.mediaDevices.getUserMedia) {
        showScanError("Браузер не дозволяє доступ до камери. Відкрийте сторінку через HTTPS або localhost.");
        return;
    }

    const detector = new BarcodeDetector({
        formats: ["qr_code", "ean_13", "ean_8", "code_128", "code_39", "upc_a", "upc_e"]
    });

    const video = document.getElementById("cameraPreview");

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
                await scanCode(value, "Camera");
                stopCameraScanner();
            }
        }, 1000);
    } catch {
        showScanError("Не вдалося увімкнути камеру. Дозвольте доступ до камери або використайте ручне введення коду.");
    }
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

    const video = document.getElementById("cameraPreview");

    if (video) {
        video.style.display = "none";
    }
}

async function loadScanLogs() {
    const response = await fetch(`${scanCodesApi}/logs`);

    if (!response.ok) {
        return;
    }

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
    resultText.className = "scan-message scan-message-error mt-3";
}

function showScanError(message) {
    const container = document.getElementById("scanResult");
    container.innerHTML = `<div class="scan-message scan-message-error">${message}</div>`;
}

loadFoodsForSelect();
loadScanLogs();
