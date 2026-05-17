const authApi = "/api/auth";
const authStorageKey = "mealPlannerApiUser";

function getCurrentApiUser() {
    const savedUser = localStorage.getItem(authStorageKey);

    if (!savedUser) {
        return null;
    }

    return JSON.parse(savedUser);
}

function saveCurrentApiUser(user) {
    localStorage.setItem(authStorageKey, JSON.stringify(user));
    updateAuthView();
    fillCurrentUserId();
}

async function registerApiUser() {
    const request = {
        name: document.getElementById("authName").value.trim(),
        email: document.getElementById("authEmail").value.trim(),
        password: document.getElementById("authPassword").value
    };

    if (!request.name || !request.email || !request.password) {
        showAuthMessage("Для реєстрації введіть ім'я, email і пароль.", true);
        return;
    }

    await sendAuthRequest(`${authApi}/register`, request);
}

async function loginApiUser() {
    const request = {
        email: document.getElementById("authEmail").value.trim(),
        password: document.getElementById("authPassword").value
    };

    if (!request.email || !request.password) {
        showAuthMessage("Для входу введіть email і пароль.", true);
        return;
    }

    await sendAuthRequest(`${authApi}/login`, request);
}

async function sendAuthRequest(url, request) {
    const response = await fetch(url, {
        method: "POST",
        headers: {
            "Accept": "application/json",
            "Content-Type": "application/json"
        },
        body: JSON.stringify(request)
    });

    const data = await response.json();

    if (!response.ok || !data.success) {
        showAuthMessage(data.message ?? "Не вдалося виконати запит.", true);
        return;
    }

    saveCurrentApiUser(data);
    showAuthMessage(`${data.message} Ви увійшли як ${data.userName} (${data.role}).`, false);
}

function logoutApiUser() {
    localStorage.removeItem(authStorageKey);
    updateAuthView();
    fillCurrentUserId();
    showAuthMessage("Ви вийшли з API-клієнта.", false);
}

function updateAuthView() {
    renderAuthStatus();
    toggleAuthorizedContent();
}

function toggleAuthorizedContent() {
    const authGate = document.getElementById("authGate");
    const appContent = document.getElementById("appContent");
    const user = getCurrentApiUser();

    if (authGate) {
        authGate.hidden = user !== null;
    }

    if (appContent) {
        appContent.hidden = user === null;
    }
}

function renderAuthStatus() {
    const statusElements = document.querySelectorAll(".auth-status");
    const user = getCurrentApiUser();

    statusElements.forEach(status => {
        if (!user) {
            status.innerHTML = "<span class='auth-badge'>Гість</span> Авторизуйтеся, щоб перейти до роботи з API-клієнтом.";
            return;
        }

        status.innerHTML = `
            <span class='auth-badge'>${user.role}</span>
            Авторизовано: <b>${user.userName}</b> (${user.email})
            <button type="button" class="secondary auth-logout-btn" onclick="logoutApiUser()">Вийти</button>
        `;
    });
}

function showAuthMessage(message, isError) {
    const element = document.getElementById("authMessage");

    if (!element) {
        return;
    }

    element.textContent = message;
    element.className = isError ? "error" : "success";
}

function fillCurrentUserId() {
    const userIdInput = document.getElementById("userIdInput");
    const user = getCurrentApiUser();

    if (!userIdInput) {
        return;
    }

    if (user?.userId) {
        userIdInput.value = user.userId;
    }
}

function getCurrentApiUserId() {
    return getCurrentApiUser()?.userId ?? null;
}

updateAuthView();
