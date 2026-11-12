-- ============================================================
--  FoodExpress - add more menu items
--  Target DB : Data\FoodExpressDb\FoodExpress.db3  (Windows dev)
--              /data/user/0/com.companyname.foodorderingapp/files/
--              FoodExpressDb/FoodExpress.db3        (Android)
--
--  Table "Food" columns:
--    Id        INTEGER PRIMARY KEY
--    Name      TEXT
--    Description TEXT
--    Price     REAL
--    Category  TEXT   -> must match a Name in the FoodCategory table
--    Image     TEXT   -> file name in Resources\Images
--    IsPopular INTEGER (0 = no, 1 = yes)
--
--  Current rows are Id 1-15, so new items start at 16.
--  OR REPLACE makes this script safe to run more than once.
-- ============================================================

BEGIN TRANSACTION;

-- --- New filter categories (skip if you only use the 5 existing ones) ---
INSERT OR REPLACE INTO FoodCategory (Name, IconKey) VALUES ('Chicken', 'chicken');
INSERT OR REPLACE INTO FoodCategory (Name, IconKey) VALUES ('Salads',  'salad');

-- --- New food items ---
INSERT OR REPLACE INTO Food (Id, Name, Description, Price, Category, Image, IsPopular) VALUES
    (16, 'Crispy Chicken Wings',  'Six golden wings tossed in smoky BBQ sauce',              6.99, 'Chicken',  'food_chicken_burger.jpg', 1),
    (17, 'Spicy Chicken Wrap',    'Grilled chicken, jalapenos and garlic sauce in a warm wrap', 5.99, 'Chicken',  'food_chicken_burger.jpg', 0),
    (18, 'Hawaiian Pizza',        'Ham, pineapple and extra mozzarella',                     9.49, 'Pizza',    'food_pepperoni.jpg',      0),
    (19, 'Garlic Bread',          'Toasted baguette with garlic butter and herbs',           3.49, 'Pizza',    'food_margherita.jpg',     0),
    (20, 'Caesar Salad',          'Romaine, parmesan, croutons and Caesar dressing',         4.99, 'Salads',   'food_veggie_pizza.jpg',   0),
    (21, 'Beef Udon Bowl',        'Thick udon noodles in a rich beef broth',                 7.49, 'Asian',    'food_beef_noodles.jpg',   0),
    (22, 'Shrimp Tempura Roll',   'Crispy shrimp, avocado and spicy mayo, 8 pieces',         8.99, 'Asian',    'food_salmon_sushi.jpg',   1),
    (23, 'Mango Smoothie',        'Blended mango and yogurt, no added sugar',                3.49, 'Drinks',   'food_orange_juice.jpg',   0),
    (24, 'Caramel Cheesecake',    'Creamy baked cheesecake with caramel drizzle',            4.99, 'Desserts', 'food_lava_cake.jpg',      0),
    (25, 'Strawberry Sundae',     'Vanilla ice cream, strawberry sauce and nuts',            3.49, 'Desserts', 'food_ice_cream.jpg',      0);

COMMIT;

-- --- Check the result ---
SELECT Id, Name, Category, Price, IsPopular FROM Food ORDER BY Id;
