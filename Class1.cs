using System;
using System.ServiceModel;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace MortgagePlugin1
{
    public class Class1 : IPlugin
    {
        public void Execute(IServiceProvider serviceProvider)
        {
            ITracingService tracingService = (ITracingService)serviceProvider.GetService(typeof(ITracingService));
            IPluginExecutionContext context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));
            IOrganizationServiceFactory serviceFactory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
            IOrganizationService service = serviceFactory.CreateOrganizationService(context.UserId);

            tracingService.Trace("Plugin execution started.");

            try
            {
                if (context.InputParameters.Contains("Target") && context.InputParameters["Target"] is Entity entity)
                {
                    if (entity.LogicalName != "contoso_mortgageapplication")
                    {
                        tracingService.Trace("Entity is not of type 'contoso_mortgageapplication'");
                        return;
                    }

                    if (!entity.Contains("contoso_applicationstatus"))
                    {
                        tracingService.Trace("contoso_applicationstatus option set is missing.");
                        return;
                    }

                    const int APPROVED_STATUS_VALUE = 463270002; // Replace this with the actual value

                    if (entity.GetAttributeValue<OptionSetValue>("contoso_applicationstatus")?.Value != APPROVED_STATUS_VALUE)
                    {
                        tracingService.Trace("The status is not 'Approved'. Exiting the plugin.");
                        return;
                    }

                    // Retrieve full entity if some fields are missing
                    if (!entity.Contains("contoso_mortgageterm") || !entity.Contains("contoso_mortgageamount") || !entity.Contains("contoso_riskscores"))
                    {
                        entity = service.Retrieve("contoso_mortgageapplication", entity.Id, new ColumnSet("contoso_mortgageterm", "contoso_mortgageamount", "contoso_riskscores", "contoso_salestaxrate"));
                        tracingService.Trace("Retrieved the full entity to get all required fields.");
                    }

                    // Ensure all required fields are present
                    if (entity.Contains("contoso_mortgageterm") && entity.Contains("contoso_mortgageamount") && entity.Contains("contoso_riskscores") && entity.Contains("contoso_salestaxrate"))
                    {
                        int durationMonths = (int)entity["contoso_mortgageterm"];
                        Money mortgageAmount = entity.GetAttributeValue<Money>("contoso_mortgageamount");
                        decimal Pv = mortgageAmount.Value;
                        int riskScoreValue = entity.GetAttributeValue<int>("contoso_riskscores"); // Use int for risk score
                        decimal salesTaxRate = entity.GetAttributeValue<decimal>("contoso_salestaxrate");

                        DateTime approvalStartDate = DateTime.Now;

                        tracingService.Trace("Duration in months: {0}", durationMonths);

                        // APR Calculation
                        int baseApr = 20;
                        decimal riskScore = (decimal)Math.Log10(riskScoreValue); // Convert riskScoreValue to decimal and use Log10
                        int margin = 20;
                        decimal finalApr = baseApr + margin + riskScore + salesTaxRate;

                        decimal monthlyInterestRate = finalApr / (100 * 12);

                        decimal monthlyPayment = (Pv * monthlyInterestRate) / (1 - (decimal)Math.Pow((double)(1 + monthlyInterestRate), -durationMonths));

                        // Create monthly payment records
                        for (int i = 1; i <= durationMonths; i++)
                        {
                            Entity payment = new Entity("contoso_mortgagepayment");
                            payment["contoso_name"] = entity.GetAttributeValue<string>("contoso_name");
                            payment["contoso_mortgageapplication"] = entity.ToEntityReference();
                            DateTime paymentDueDate = approvalStartDate.AddMonths(i);
                            payment["contoso_duedate"] = paymentDueDate;
                            payment["contoso_paymentamount"] = new Money(monthlyPayment);
                            payment["contoso_paymentnumber"] = $"Payment {i}";

                            if (entity.Contains("ownerid"))
                            {
                                payment["ownerid"] = entity.GetAttributeValue<EntityReference>("ownerid");
                            }

                            service.Create(payment);
                            tracingService.Trace($"Payment record created: Due Date {paymentDueDate}, Amount {monthlyPayment}");
                        }

                        tracingService.Trace("Payment records creation completed successfully.");
                    }
                    else
                    {
                        tracingService.Trace("One or more required fields (contoso_mortgageterm, contoso_mortgageamount, contoso_riskscores, contoso_salestaxrate) are missing.");
                    }
                }
                else
                {
                    tracingService.Trace("Target is either missing or not of type Entity.");
                }
            }
            catch (FaultException<OrganizationServiceFault> ex)
            {
                tracingService.Trace("FaultException: {0}", ex.ToString());
                throw new InvalidPluginExecutionException("An error occurred in CreatePaymentRecordsPlugin.", ex);
            }
            catch (Exception ex)
            {
                tracingService.Trace("Exception: {0}", ex.ToString());
                throw new InvalidPluginExecutionException("An unexpected error occurred in CreatePaymentRecordsPlugin.", ex);
            }
            finally
            {
                tracingService.Trace("Plugin execution completed.");
            }
        }
    }
}
