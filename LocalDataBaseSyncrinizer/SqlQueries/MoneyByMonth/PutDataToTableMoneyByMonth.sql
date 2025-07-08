INSERT INTO money_by_month
(`event_date`, `total_in`, `month_in`, `dividend`, `dosrochnoe`, `deals_sum`, `brok_comission`, `money_sum`)
	VALUES 
    (@values)
	AS aliased
ON DUPLICATE KEY UPDATE 
	`total_in` = aliased.`total_in`,  
    `month_in` = aliased.`month_in`, 
    `dividend` = aliased.`dividend`, 
    `dosrochnoe` = aliased.`dosrochnoe`, 
	`deals_sum` = aliased.`deals_sum`,
    `brok_comission` = aliased.`brok_comission`,
    `money_sum` = aliased.`money_sum`