INSERT INTO wish_list
		(`seccode`, `wish_level`, `description`)
	VALUES 
    (@values)
	AS aliased
ON DUPLICATE KEY UPDATE 
	`wish_level` = aliased.`wish_level`,  
	`description` = aliased.`description`;