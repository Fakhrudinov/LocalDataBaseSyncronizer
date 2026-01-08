INSERT INTO money_spent_by_month
(`event_date`, `total`, `appartment`, `electricity`, `internet`, `phone`, `transport`, `supermarket`, `marketplaces`)
	VALUES 
    (@values)
	AS aliased
ON DUPLICATE KEY UPDATE 
	`total` = aliased.`total`,  
    `appartment` = aliased.`appartment`, 
    `electricity` = aliased.`electricity`, 
    `internet` = aliased.`internet`, 
	`phone` = aliased.`phone`, 
	`transport` = aliased.`transport`, 
	`supermarket` = aliased.`supermarket`, 
	`marketplaces` = aliased.`marketplaces`;