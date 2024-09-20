**Purpose**:
The purpose of this Dynamics 365 plugin is to automate the creation of mortgage payment records when a mortgage application is approved. The plugin first checks if the triggering entity is of type `contoso_mortgageapplication` and verifies if the `contoso_applicationstatus` field has a status value indicating approval (in this case, `463270002`). If the status is not "Approved," the plugin exits early.

Once the mortgage application is approved, the plugin ensures that required fields like the mortgage term (`contoso_mortgageterm`), mortgage amount (`contoso_mortgageamount`), risk scores (`contoso_riskscores`), and sales tax rate (`contoso_salestaxrate`) are present. If any are missing, it retrieves the full entity with these fields.

The plugin then calculates the Annual Percentage Rate (APR) based on a base APR, a margin, the logarithm of the risk score, and the sales tax rate. Using this APR, it computes the monthly interest rate and the monthly payment amount using standard mortgage formulas.

It proceeds to create monthly payment records for the entire mortgage term. Each payment record includes details such as the due date, payment amount, the name of the mortgage, and the mortgage application reference. If an owner is associated with the mortgage, it assigns the same owner to the payment record. The plugin traces each step for debugging and troubleshooting, ensuring that any errors encountered are logged and properly handled.

This automation saves time and reduces human error by generating payment schedules programmatically upon mortgage approval, ensuring consistency and efficiency in managing mortgage applications.
