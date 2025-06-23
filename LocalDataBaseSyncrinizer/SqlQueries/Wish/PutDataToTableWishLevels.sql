INSERT INTO wish_levels
		(`level`, `weight`)
	VALUES 
    (@values)
	AS aliased
ON DUPLICATE KEY UPDATE `weight`=aliased.`weight`;