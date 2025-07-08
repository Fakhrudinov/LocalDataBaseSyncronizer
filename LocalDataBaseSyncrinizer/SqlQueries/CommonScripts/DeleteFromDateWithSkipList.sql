DELETE 
	FROM @tableName
	WHERE (`event_date` > @event_date 
		AND `event_date` not in (@values));