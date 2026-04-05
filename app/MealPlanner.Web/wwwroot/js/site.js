document.addEventListener("DOMContentLoaded", function () {
    const plannerRoot = document.querySelector("[data-planner-root]");

    if (!plannerRoot) {
        return;
    }

    const mealsPerDayInput = plannerRoot.querySelector("#MealsPerDay");
    const mealButtons = plannerRoot.querySelectorAll("[data-meal-count]");
    const mealCards = plannerRoot.querySelectorAll("[data-meal-card]");

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
});