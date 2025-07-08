INSERT INTO bank_deposits
(`id`, `isopen`, `event_date`, `date_close`, `name`, `placed_name`, `percent`, `summ`, `income_summ`)
	VALUES 
    (@values)
	AS aliased
ON DUPLICATE KEY UPDATE 
	`isopen` = aliased.`isopen`,  
    `event_date` = aliased.`event_date`, 
    `date_close` = aliased.`date_close`, 
    `name` = aliased.`name`, 
	`placed_name` = aliased.`placed_name`,
    `percent` = aliased.`percent`,
    `summ` = aliased.`summ`,
    `income_summ` = aliased.`income_summ`;