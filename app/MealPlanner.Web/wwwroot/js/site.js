document.addEventListener("DOMContentLoaded", function () {
    const plannerRoot = document.querySelector("[data-planner-root]");

    if (plannerRoot) {
        initPlannerMealCount(plannerRoot);
        initPlannerPickers(plannerRoot);
        initPlannerExportCopy(plannerRoot);
    }
});

function initPlannerMealCount(plannerRoot) {
    const mealsPerDayInput = plannerRoot.querySelector("#MealsPerDay");
    const mealButtons = plannerRoot.querySelectorAll("[data-meal-count]");
    const mealCards = plannerRoot.querySelectorAll("[data-meal-card]");

    if (!mealsPerDayInput) {
        return;
    }

    function applyMealCount(count) {
        mealButtons.forEach((button) => {
            const value = Number(button.dataset.mealCount);
            button.classList.toggle("meal-count-btn-active", value === count);
        });

        mealCards.forEach((card) => {
            const mealNo = Number(card.dataset.mealNo);
            const shouldShow = mealNo <= count;
            card.classList.toggle("planner-meal-hidden", !shouldShow);
        });
    }

    mealButtons.forEach((button) => {
        button.addEventListener("click", function () {
            const count = Number(button.dataset.mealCount);
            mealsPerDayInput.value = count;
            applyMealCount(count);
        });
    });

    const initialCount = Number(mealsPerDayInput.value || 2);
    applyMealCount(initialCount);
}

function initPlannerPickers(plannerRoot) {
    const modalElements = {
        protein: document.getElementById("proteinPickerModal"),
        carb: document.getElementById("carbPickerModal"),
        fat: document.getElementById("fatPickerModal")
    };

    const modals = {
        protein: modalElements.protein ? bootstrap.Modal.getOrCreateInstance(modalElements.protein) : null,
        carb: modalElements.carb ? bootstrap.Modal.getOrCreateInstance(modalElements.carb) : null,
        fat: modalElements.fat ? bootstrap.Modal.getOrCreateInstance(modalElements.fat) : null
    };

    const pickerState = {
        type: null,
        targetInputId: null,
        targetLabelId: null
    };

    plannerRoot.querySelectorAll("[data-picker-open]").forEach((button) => {
        button.addEventListener("click", function () {
            const type = button.dataset.pickerOpen;
            const targetInputId = button.dataset.targetInput;
            const targetLabelId = button.dataset.targetLabel;

            pickerState.type = type;
            pickerState.targetInputId = targetInputId;
            pickerState.targetLabelId = targetLabelId;

            modals[type]?.show();
        });
    });

    document.querySelectorAll("[data-picker-select]").forEach((button) => {
        button.addEventListener("click", function () {
            const type = button.dataset.pickerSelect;

            if (pickerState.type !== type) {
                return;
            }

            const input = document.getElementById(pickerState.targetInputId);
            const label = document.getElementById(pickerState.targetLabelId);

            if (input) {
                input.value = button.dataset.foodId;
            }

            if (label) {
                label.textContent = button.dataset.foodLabel;
            }

            modals[type]?.hide();
        });
    });

    document.querySelectorAll("[data-picker-clear]").forEach((button) => {
        button.addEventListener("click", function () {
            const type = button.dataset.pickerClear;

            if (pickerState.type !== type) {
                return;
            }

            const input = document.getElementById(pickerState.targetInputId);
            const label = document.getElementById(pickerState.targetLabelId);

            if (input) {
                input.value = "";
            }

            if (label) {
                label.textContent = "Не вибрано";
            }

            modals[type]?.hide();
        });
    });
}

function initPlannerExportCopy(plannerRoot) {
    const copyButton = document.getElementById("copyExportBtn");
    const exportText = document.getElementById("plannerExportText");

    if (!copyButton || !exportText) {
        return;
    }

    copyButton.addEventListener("click", async function () {
        try {
            await navigator.clipboard.writeText(exportText.value);
            copyButton.textContent = "Скопійовано";
        } catch {
            exportText.select();
            document.execCommand("copy");
            copyButton.textContent = "Скопійовано";
        }

        setTimeout(() => {
            copyButton.textContent = "Копіювати текст";
        }, 1500);
    });
}