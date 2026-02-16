-- Meal Planner DB schema (PostgreSQL)
-- Synced with updated ERD (full names + quantity unit)

BEGIN;

-- For UUID generation
CREATE EXTENSION IF NOT EXISTS pgcrypto;

-- (optional) reset tables to re-run script safely
DROP TABLE IF EXISTS export CASCADE;
DROP TABLE IF EXISTS shop_item CASCADE;
DROP TABLE IF EXISTS shop_list CASCADE;
DROP TABLE IF EXISTS meal_item CASCADE;
DROP TABLE IF EXISTS meal CASCADE;
DROP TABLE IF EXISTS plan CASCADE;
DROP TABLE IF EXISTS food CASCADE;
DROP TABLE IF EXISTS icon CASCADE;
DROP TABLE IF EXISTS macro_log CASCADE;
DROP TABLE IF EXISTS configuration CASCADE;
DROP TABLE IF EXISTS macro CASCADE;
DROP TABLE IF EXISTS profile CASCADE;
DROP TABLE IF EXISTS users CASCADE;

-- =========================
-- users
-- =========================
CREATE TABLE users (
    id         uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    email      varchar NOT NULL UNIQUE,
    name       varchar NOT NULL,
    created_at timestamp NOT NULL DEFAULT now()
);

-- =========================
-- profile (1:1 with users)
-- =========================
CREATE TABLE profile (
    id         uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id    uuid NOT NULL UNIQUE REFERENCES users(id) ON DELETE CASCADE,
    weight_kg  numeric,
    goal       varchar,
    updated_at timestamp NOT NULL DEFAULT now()
);

-- =========================
-- macro
-- =========================
CREATE TABLE macro (
    id         uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id    uuid NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    protein_g  int NOT NULL,
    carbs_g    int NOT NULL,
    fat_g      int NOT NULL,
    mode       varchar NOT NULL,
    created_at timestamp NOT NULL DEFAULT now(),
    CONSTRAINT macro_nonneg CHECK (protein_g >= 0 AND carbs_g >= 0 AND fat_g >= 0)
);

-- =========================
-- configuration (1:1 with users, active_macro_id optional)
-- =========================
CREATE TABLE configuration (
    user_id         uuid PRIMARY KEY REFERENCES users(id) ON DELETE CASCADE,
    lang            varchar NOT NULL,
    meals_per_day   int NOT NULL,
    active_macro_id uuid REFERENCES macro(id) ON DELETE SET NULL,
    updated_at      timestamp NOT NULL DEFAULT now(),
    CONSTRAINT configuration_meals_per_day_check CHECK (meals_per_day > 0)
);

-- =========================
-- macro_log
-- =========================
CREATE TABLE macro_log (
    id         uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id    uuid NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    macro_id   uuid REFERENCES macro(id) ON DELETE SET NULL,
    event      varchar NOT NULL,
    protein_g  int NOT NULL,
    carbs_g    int NOT NULL,
    fat_g      int NOT NULL,
    changed_at timestamp NOT NULL DEFAULT now(),
    CONSTRAINT macro_log_nonneg CHECK (protein_g >= 0 AND carbs_g >= 0 AND fat_g >= 0)
);

-- =========================
-- icon
-- =========================
CREATE TABLE icon (
    id    uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    code  varchar NOT NULL UNIQUE,
    emoji varchar NOT NULL
);

-- =========================
-- food (belongs to user, optional icon)
-- =========================
CREATE TABLE food (
    id               uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id          uuid NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    icon_id          uuid REFERENCES icon(id) ON DELETE SET NULL,
    name             varchar NOT NULL,
    protein_per_100  numeric NOT NULL,
    carbs_per_100    numeric NOT NULL,
    fat_per_100      numeric NOT NULL,
    kcal_per_100     numeric NOT NULL,
    is_custom        boolean NOT NULL DEFAULT true,
    updated_at       timestamp NOT NULL DEFAULT now(),
    CONSTRAINT food_nonneg CHECK (
        protein_per_100 >= 0 AND carbs_per_100 >= 0 AND fat_per_100 >= 0 AND kcal_per_100 >= 0
    )
);

-- =========================
-- plan (belongs to user, optionally uses macro)
-- =========================
CREATE TABLE plan (
    id         uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id    uuid NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    macro_id   uuid REFERENCES macro(id) ON DELETE SET NULL,
    days       int NOT NULL,
    status     varchar NOT NULL,
    created_at timestamp NOT NULL DEFAULT now(),
    CONSTRAINT plan_days_check CHECK (days > 0)
);

-- =========================
-- shop_list (belongs to plan)
-- =========================
CREATE TABLE shop_list (
    id         uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    plan_id    uuid NOT NULL REFERENCES plan(id) ON DELETE CASCADE,
    days       int NOT NULL,
    created_at timestamp NOT NULL DEFAULT now(),
    CONSTRAINT shop_list_days_check CHECK (days > 0)
);

-- =========================
-- meal (belongs to plan)
-- =========================
CREATE TABLE meal (
    id      uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    plan_id uuid NOT NULL REFERENCES plan(id) ON DELETE CASCADE,
    day_no  int NOT NULL,
    meal_no int NOT NULL,
    name    varchar NOT NULL,
    CONSTRAINT meal_day_check CHECK (day_no > 0),
    CONSTRAINT meal_no_check CHECK (meal_no > 0)
);

-- =========================
-- meal_item (M:N between meal and food)
-- quantity_value + quantity_unit (g/ml/l)
-- =========================
CREATE TABLE meal_item (
    id             uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    meal_id        uuid NOT NULL REFERENCES meal(id) ON DELETE CASCADE,
    food_id        uuid NOT NULL REFERENCES food(id) ON DELETE RESTRICT,
    quantity_value numeric NOT NULL,
    quantity_unit  varchar NOT NULL,
    is_locked      boolean NOT NULL DEFAULT false,
    CONSTRAINT meal_item_qty_check CHECK (quantity_value > 0),
    CONSTRAINT meal_item_unit_check CHECK (quantity_unit IN ('g', 'ml', 'l'))
);

-- =========================
-- shop_item (M:N between shop_list and food)
-- total_quantity_value + quantity_unit (g/ml/l)
-- =========================
CREATE TABLE shop_item (
    id                  uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    list_id             uuid NOT NULL REFERENCES shop_list(id) ON DELETE CASCADE,
    food_id             uuid NOT NULL REFERENCES food(id) ON DELETE RESTRICT,
    total_quantity_value numeric NOT NULL,
    quantity_unit       varchar NOT NULL,
    CONSTRAINT shop_item_qty_check CHECK (total_quantity_value > 0),
    CONSTRAINT shop_item_unit_check CHECK (quantity_unit IN ('g', 'ml', 'l'))
);

-- =========================
-- export (optional refs to plan / list)
-- =========================
CREATE TABLE export (
    id         uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id    uuid NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    type       varchar NOT NULL,
    plan_id    uuid REFERENCES plan(id) ON DELETE SET NULL,
    list_id    uuid REFERENCES shop_list(id) ON DELETE SET NULL,
    file_url   varchar NOT NULL,
    created_at timestamp NOT NULL DEFAULT now()
);

-- =========================
-- Indexes
-- =========================
CREATE INDEX idx_profile_user_id           ON profile(user_id);
CREATE INDEX idx_configuration_active_macro ON configuration(active_macro_id);

CREATE INDEX idx_macro_user_id             ON macro(user_id);
CREATE INDEX idx_macro_log_user_id         ON macro_log(user_id);
CREATE INDEX idx_macro_log_macro_id        ON macro_log(macro_id);

CREATE INDEX idx_plan_user_id              ON plan(user_id);
CREATE INDEX idx_plan_macro_id             ON plan(macro_id);

CREATE INDEX idx_shop_list_plan_id         ON shop_list(plan_id);

CREATE INDEX idx_meal_plan_id              ON meal(plan_id);

CREATE INDEX idx_food_user_id              ON food(user_id);
CREATE INDEX idx_food_icon_id              ON food(icon_id);

CREATE INDEX idx_meal_item_meal_id         ON meal_item(meal_id);
CREATE INDEX idx_meal_item_food_id         ON meal_item(food_id);

CREATE INDEX idx_shop_item_list_id         ON shop_item(list_id);
CREATE INDEX idx_shop_item_food_id         ON shop_item(food_id);

CREATE INDEX idx_export_user_id            ON export(user_id);

COMMIT;
