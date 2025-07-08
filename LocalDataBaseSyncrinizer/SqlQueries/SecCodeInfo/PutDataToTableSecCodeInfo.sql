INSERT INTO seccode_info
(`seccode`, `secboard`, `name`, `full_name`, `isin`, `expired_date`)
	VALUES 
    (@values)
	AS aliased
ON DUPLICATE KEY UPDATE 
	`secboard` = aliased.`secboard`,  
    `name` = aliased.`name`, 
    `full_name` = aliased.`full_name`, 
    `isin` = aliased.`isin`, 
	`expired_date` = aliased.`expired_date`;