DELETE 
	FROM `bank_deposits`
	WHERE (`id` >= @id
		AND `id` not in (@values));