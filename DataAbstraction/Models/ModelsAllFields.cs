
namespace DataAbstraction.Models
{
	//internal class ModelsAllFields
	//{

	//}
}

/*



get last records - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - 
`deals` 
	(`id`, `event_date`, `seccode`, `secboard`, `av_price`, `pieces`, `comission`, `nkd`)
`incoming` 
	(`id`, `event_date`, `seccode`, `secboard`, `category`, `value`, `comission`)


last 12 records from last record to past and all to future - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - 
`money_by_month` 
	(`event_date`, `total_in`, `month_in`, `dividend`, `dosrochnoe`, `deals_sum`, `brok_comission`, `money_sum`)
`money_spent_by_month` 
	(`event_date`, `total`, `appartment`, `electricity`, `internet`, `phone`)
`bank_deposits` 
	(`id`, `isopen`, `event_date`, `date_close`, `name`, `placed_name`, `percent`, `summ`, `income_summ`)



full table check- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - 
`seccode_info` 
	(`seccode`, `secboard`, `name`, `full_name`, `isin`, `expired_date`)
`wish_levels` 
	(`level`, `weight`)
`wish_list` 
	(`seccode`, `wish_level`, `description`)


dont do this? - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - 
`sec_volume` 
	(`seccode`, `secboard`, `pieces_2025`, `av_price_2025`, `volume_2025`)






*/